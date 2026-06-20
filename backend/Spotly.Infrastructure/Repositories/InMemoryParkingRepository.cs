using System.Data;
using Microsoft.EntityFrameworkCore;
using Spotly.Domain.Entities;
using Spotly.Domain.Interfaces;
using Spotly.Domain.Rules;
using Spotly.Infrastructure.Persistence;

namespace Spotly.Infrastructure.Repositories;

public sealed class InMemoryParkingRepository(IDbContextFactory<SpotlyDbContext> factory, TimeProvider clock) : IParkingRepository
{
    private static readonly Lock Gate = new();

    public Task<IEnumerable<ParkingSpot>> GetSpotsAsync(string locationId, DateOnly date)
    {
        lock (Gate)
        {
            using var db = factory.CreateDbContext();
            var now = clock.GetUtcNow().UtcDateTime;
            var bookings = db.ParkingBookings.AsNoTracking()
                .Where(x => x.BookingDate == date && x.Status == BookingStatus.Active)
                .ToList();
            var result = db.ParkingSpots.AsNoTracking()
                .Where(x => x.LocationId == locationId)
                .ToList()
                .Select(spot => spot with
                {
                    Status = bookings.Any(x => x.SpotId == spot.SpotId && x.LockedUntil == null)
                        ? ResourceStatus.Occupied
                        : bookings.Any(x => x.SpotId == spot.SpotId && x.LockedUntil > now)
                            ? ResourceStatus.Pending
                            : spot.Status,
                })
                .ToArray();
            return Task.FromResult<IEnumerable<ParkingSpot>>(result);
        }
    }

    public Task<ParkingSpot?> GetSpotByIdAsync(string spotId)
    {
        using var db = factory.CreateDbContext();
        return Task.FromResult(db.ParkingSpots.AsNoTracking().FirstOrDefault(x => x.SpotId == spotId));
    }

    public Task<IEnumerable<ParkingBooking>> GetBookingsByDateAsync(string locationId, DateOnly date)
    {
        using var db = factory.CreateDbContext();
        var ids = db.ParkingSpots.Where(x => x.LocationId == locationId).Select(x => x.SpotId).ToHashSet();
        return Task.FromResult<IEnumerable<ParkingBooking>>(db.ParkingBookings.AsNoTracking()
            .Where(x => x.BookingDate == date && ids.Contains(x.SpotId))
            .ToArray());
    }

    public Task<ParkingBooking?> GetActiveBookingAsync(string userId, DateOnly date)
    {
        using var db = factory.CreateDbContext();
        return Task.FromResult(db.ParkingBookings.AsNoTracking().FirstOrDefault(x =>
            x.UserId == userId &&
            x.BookingDate == date &&
            x.Status == BookingStatus.Active &&
            x.LockedUntil == null));
    }

    public Task<BookingAttempt<ParkingBooking>> TryCreateBookingAsync(ParkingBooking booking)
    {
        lock (Gate)
        {
            using var db = factory.CreateDbContext();
            using var transaction = db.Database.IsRelational() ? db.Database.BeginTransaction(IsolationLevel.Serializable) : null;
            var now = clock.GetUtcNow().UtcDateTime;
            var spot = db.ParkingSpots.FirstOrDefault(x => x.SpotId == booking.SpotId);
            if (spot is null) return Task.FromResult(new BookingAttempt<ParkingBooking>(null, BookingFailure.NotFound));
            if (spot.Status is ResourceStatus.Occupied or ResourceStatus.Reserved)
                return Task.FromResult(new BookingAttempt<ParkingBooking>(null, BookingFailure.ResourceUnavailable));
            if (db.ParkingBookings.Any(x => x.UserId == booking.UserId && x.BookingDate == booking.BookingDate &&
                x.Status == BookingStatus.Active && x.LockedUntil == null))
                return Task.FromResult(new BookingAttempt<ParkingBooking>(null, BookingFailure.AlreadyBooked));
            if (db.ParkingBookings.Any(x => x.SpotId == booking.SpotId && x.BookingDate == booking.BookingDate &&
                x.Status == BookingStatus.Active && (x.LockedUntil == null || x.LockedUntil > now) && x.LockedByUserId != booking.UserId))
                return Task.FromResult(new BookingAttempt<ParkingBooking>(null, BookingFailure.ResourceUnavailable));

            var ownLock = db.ParkingBookings.FirstOrDefault(x => x.SpotId == booking.SpotId &&
                x.BookingDate == booking.BookingDate &&
                x.Status == BookingStatus.Active &&
                x.LockedByUserId == booking.UserId &&
                x.LockedUntil > now);

            if (ownLock is null)
            {
                booking.CreatedAtUtc = booking.CreatedAtUtc == default ? now : booking.CreatedAtUtc;
                db.ParkingBookings.Add(booking);
            }
            else
            {
                ownLock.LockedUntil = null;
                ownLock.LockedByUserId = null;
                ownLock.CheckInOpensAtUtc = booking.CheckInOpensAtUtc;
                ownLock.CheckInDeadlineUtc = booking.CheckInDeadlineUtc;
                booking = ownLock;
            }

            try
            {
                db.SaveChanges();
                transaction?.Commit();
                return Task.FromResult(new BookingAttempt<ParkingBooking>(booking, BookingFailure.None));
            }
            catch (DbUpdateException)
            {
                return Task.FromResult(new BookingAttempt<ParkingBooking>(null, BookingFailure.ResourceUnavailable));
            }
        }
    }

    public Task<CancellationOutcome> CancelBookingAsync(string bookingId, string userId, DateTime currentLocalTime)
    {
        lock (Gate)
        {
            using var db = factory.CreateDbContext();
            var booking = db.ParkingBookings.FirstOrDefault(x => x.BookingId == bookingId && x.UserId == userId && x.Status == BookingStatus.Active);
            if (booking is null) return Task.FromResult(new CancellationOutcome(false));
            booking.Status = BookingRules.IsFreeCancellation(booking.BookingDate, currentLocalTime) ? BookingStatus.Cancelled : BookingStatus.NoShow;
            db.SaveChanges();
            return Task.FromResult(new CancellationOutcome(true, booking.Status, booking.SpotId, booking.BookingDate));
        }
    }

    public Task<bool> CheckInAsync(string bookingId, string userId, DateTime utcNow)
    {
        lock (Gate)
        {
            using var db = factory.CreateDbContext();
            var booking = db.ParkingBookings.FirstOrDefault(x =>
                x.BookingId == bookingId &&
                x.UserId == userId &&
                x.Status == BookingStatus.Active &&
                x.LockedUntil == null &&
                x.CheckInOpensAtUtc <= utcNow &&
                x.CheckInDeadlineUtc >= utcNow);
            if (booking is null) return Task.FromResult(false);
            booking.CheckedInAtUtc = utcNow;
            db.SaveChanges();
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryAcquireLockAsync(string spotId, string userId, DateOnly bookingDate, TimeSpan lockDuration)
    {
        lock (Gate)
        {
            using var db = factory.CreateDbContext();
            using var transaction = db.Database.IsRelational() ? db.Database.BeginTransaction(IsolationLevel.Serializable) : null;
            var now = clock.GetUtcNow().UtcDateTime;
            var spot = db.ParkingSpots.FirstOrDefault(x => x.SpotId == spotId);
            if (spot is null || spot.Status != ResourceStatus.Available) return Task.FromResult(false);
            if (db.ParkingBookings.Any(x => x.UserId == userId && x.BookingDate == bookingDate && x.Status == BookingStatus.Active))
                return Task.FromResult(false);
            if (db.ParkingBookings.Any(x =>
                x.SpotId == spotId &&
                x.BookingDate == bookingDate &&
                x.Status == BookingStatus.Active &&
                (x.LockedUntil == null || x.LockedUntil > now)))
                return Task.FromResult(false);

            db.ParkingBookings.Add(new ParkingBooking
            {
                SpotId = spotId,
                UserId = userId,
                BookingDate = bookingDate,
                Status = BookingStatus.Active,
                CreatedAtUtc = now,
                LockedUntil = now.Add(lockDuration),
                LockedByUserId = userId,
            });

            try
            {
                db.SaveChanges();
                transaction?.Commit();
                return Task.FromResult(true);
            }
            catch (DbUpdateException)
            {
                return Task.FromResult(false);
            }
        }
    }

    public Task<IReadOnlyList<ReleasedResource>> ReleaseExpiredLocksAsync(DateTime utcNow) => ReleaseAsync(
        x => x.Status == BookingStatus.Active && x.LockedUntil != null && x.LockedUntil < utcNow,
        BookingStatus.Cancelled);

    public Task<IReadOnlyList<ReleasedResource>> ReleaseNoShowsAsync(DateTime utcNow) => ReleaseAsync(
        x => x.Status == BookingStatus.Active && x.LockedUntil == null && x.CheckedInAtUtc == null && x.CheckInDeadlineUtc < utcNow,
        BookingStatus.NoShow);

    private Task<IReadOnlyList<ReleasedResource>> ReleaseAsync(Func<ParkingBooking, bool> predicate, BookingStatus status)
    {
        lock (Gate)
        {
            using var db = factory.CreateDbContext();
            var expired = db.ParkingBookings.Where(predicate).ToList();
            foreach (var booking in expired) booking.Status = status;
            db.SaveChanges();
            return Task.FromResult<IReadOnlyList<ReleasedResource>>(expired
                .Select(x => new ReleasedResource(x.SpotId, x.BookingDate))
                .ToArray());
        }
    }
}
