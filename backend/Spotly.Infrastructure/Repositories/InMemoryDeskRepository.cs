using Microsoft.EntityFrameworkCore;
using Spotly.Domain.Entities;
using Spotly.Domain.Interfaces;
using Spotly.Domain.Rules;
using Spotly.Infrastructure.Persistence;

namespace Spotly.Infrastructure.Repositories;

public sealed class InMemoryDeskRepository(IDbContextFactory<SpotlyDbContext> factory) : IDeskRepository
{
    private readonly Lock _gate = new();

    public Task<IEnumerable<DeskSpot>> GetSpotsAsync(string locationId, DateOnly date)
    {
        lock (_gate)
        {
            using var db = factory.CreateDbContext();
            var now = DateTime.UtcNow;
            var bookings = db.DeskBookings.AsNoTracking().Where(x => x.BookingDate == date && x.Status == BookingStatus.Active).ToList();
            var result = db.DeskSpots.AsNoTracking().Where(x => x.LocationId == locationId).ToList().Select(spot => spot with
            {
                Status = bookings.Any(x => x.DeskId == spot.DeskId && x.LockedUntil == null) ? ResourceStatus.Occupied
                    : bookings.Any(x => x.DeskId == spot.DeskId && x.LockedUntil > now) ? ResourceStatus.Pending : spot.Status,
            }).ToArray();
            return Task.FromResult<IEnumerable<DeskSpot>>(result);
        }
    }

    public Task<DeskSpot?> GetSpotByIdAsync(string deskId)
    {
        using var db = factory.CreateDbContext();
        return Task.FromResult(db.DeskSpots.AsNoTracking().FirstOrDefault(x => x.DeskId == deskId));
    }

    public Task<IEnumerable<DeskBooking>> GetBookingsByDateAsync(string locationId, DateOnly date)
    {
        using var db = factory.CreateDbContext();
        var ids = db.DeskSpots.Where(x => x.LocationId == locationId).Select(x => x.DeskId).ToHashSet();
        return Task.FromResult<IEnumerable<DeskBooking>>(db.DeskBookings.AsNoTracking().Where(x => x.BookingDate == date && ids.Contains(x.DeskId)).ToArray());
    }

    public Task<DeskBooking?> GetActiveBookingAsync(string userId, DateOnly date)
    {
        using var db = factory.CreateDbContext();
        return Task.FromResult(db.DeskBookings.AsNoTracking().FirstOrDefault(x => x.UserId == userId && x.BookingDate == date &&
            x.Status == BookingStatus.Active && x.LockedUntil == null));
    }

    public Task<BookingAttempt<DeskBooking>> TryCreateBookingAsync(DeskBooking booking, string? department)
    {
        lock (_gate)
        {
            using var db = factory.CreateDbContext();
            var now = DateTime.UtcNow;
            var desk = db.DeskSpots.FirstOrDefault(x => x.DeskId == booking.DeskId);
            if (desk is null) return Task.FromResult(new BookingAttempt<DeskBooking>(null, BookingFailure.NotFound));
            if (desk.ReservedForDepartment is not null && !string.Equals(desk.ReservedForDepartment, department, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(new BookingAttempt<DeskBooking>(null, BookingFailure.Ineligible));
            if (desk.Status is ResourceStatus.Occupied or ResourceStatus.Reserved || db.DeskBookings.Any(x => x.DeskId == booking.DeskId &&
                x.BookingDate == booking.BookingDate && x.Status == BookingStatus.Active && (x.LockedUntil == null || x.LockedUntil > now) && x.LockedByUserId != booking.UserId))
                return Task.FromResult(new BookingAttempt<DeskBooking>(null, BookingFailure.ResourceUnavailable));
            if (db.DeskBookings.Any(x => x.UserId == booking.UserId && x.BookingDate == booking.BookingDate && x.Status == BookingStatus.Active && x.LockedUntil == null))
                return Task.FromResult(new BookingAttempt<DeskBooking>(null, BookingFailure.AlreadyBooked));
            var ownLock = db.DeskBookings.FirstOrDefault(x => x.DeskId == booking.DeskId && x.BookingDate == booking.BookingDate &&
                x.Status == BookingStatus.Active && x.LockedByUserId == booking.UserId && x.LockedUntil > now);
            if (ownLock is null) db.DeskBookings.Add(booking);
            else { ownLock.LockedUntil = null; ownLock.LockedByUserId = null; ownLock.CheckInDeadlineUtc = booking.CheckInDeadlineUtc; booking = ownLock; }
            db.SaveChanges();
            return Task.FromResult(new BookingAttempt<DeskBooking>(booking, BookingFailure.None));
        }
    }

    public Task<CancellationOutcome> CancelBookingAsync(string bookingId, string userId, DateTime utcNow)
    {
        lock (_gate)
        {
            using var db = factory.CreateDbContext();
            var booking = db.DeskBookings.FirstOrDefault(x => x.BookingId == bookingId && x.UserId == userId && x.Status == BookingStatus.Active);
            if (booking is null) return Task.FromResult(new CancellationOutcome(false));
            booking.Status = BookingRules.IsFreeCancellation(booking.BookingDate, utcNow) ? BookingStatus.Cancelled : BookingStatus.NoShow;
            db.SaveChanges();
            return Task.FromResult(new CancellationOutcome(true, booking.Status, booking.DeskId, booking.BookingDate));
        }
    }

    public Task<bool> CheckInAsync(string bookingId, string userId, DateTime utcNow)
    {
        lock (_gate)
        {
            using var db = factory.CreateDbContext();
            var booking = db.DeskBookings.FirstOrDefault(x => x.BookingId == bookingId && x.UserId == userId && x.Status == BookingStatus.Active &&
                x.LockedUntil == null && x.CheckInDeadlineUtc >= utcNow);
            if (booking is null) return Task.FromResult(false);
            booking.CheckedInAtUtc = utcNow; db.SaveChanges(); return Task.FromResult(true);
        }
    }

    public Task<bool> TryAcquireLockAsync(string deskId, string userId, TimeSpan lockDuration)
    {
        lock (_gate)
        {
            using var db = factory.CreateDbContext();
            var now = DateTime.UtcNow; var date = DateOnly.FromDateTime(now);
            var desk = db.DeskSpots.FirstOrDefault(x => x.DeskId == deskId);
            if (desk is null || desk.Status != ResourceStatus.Available || db.DeskBookings.Any(x => x.DeskId == deskId && x.BookingDate == date &&
                x.Status == BookingStatus.Active && (x.LockedUntil == null || x.LockedUntil > now))) return Task.FromResult(false);
            db.DeskBookings.Add(new DeskBooking { DeskId = deskId, UserId = userId, BookingDate = date, Status = BookingStatus.Active,
                LockedUntil = now.Add(lockDuration), LockedByUserId = userId });
            db.SaveChanges(); return Task.FromResult(true);
        }
    }

    public Task<IReadOnlyList<ReleasedResource>> ReleaseExpiredLocksAsync(DateTime utcNow) => ReleaseAsync(
        x => x.Status == BookingStatus.Active && x.LockedUntil != null && x.LockedUntil < utcNow, BookingStatus.Cancelled);
    public Task<IReadOnlyList<ReleasedResource>> ReleaseNoShowsAsync(DateTime utcNow) => ReleaseAsync(
        x => x.Status == BookingStatus.Active && x.LockedUntil == null && x.CheckedInAtUtc == null && x.CheckInDeadlineUtc < utcNow, BookingStatus.NoShow);

    private Task<IReadOnlyList<ReleasedResource>> ReleaseAsync(Func<DeskBooking, bool> predicate, BookingStatus status)
    {
        lock (_gate)
        {
            using var db = factory.CreateDbContext(); var expired = db.DeskBookings.Where(predicate).ToList();
            foreach (var booking in expired) booking.Status = status; db.SaveChanges();
            return Task.FromResult<IReadOnlyList<ReleasedResource>>(expired.Select(x => new ReleasedResource(x.DeskId, x.BookingDate)).ToArray());
        }
    }
}
