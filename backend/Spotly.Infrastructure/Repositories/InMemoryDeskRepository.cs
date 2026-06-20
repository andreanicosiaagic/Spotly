using Spotly.Domain.Entities;
using Spotly.Domain.Interfaces;
using Spotly.Infrastructure.Seed;

namespace Spotly.Infrastructure.Repositories;

public class InMemoryDeskRepository : IDeskRepository
{
    private readonly List<DeskSpot> _spots = SeedData.DeskSpots();
    private readonly List<DeskBooking> _bookings = [];
    private readonly Lock _lock = new();

    public Task<IEnumerable<DeskSpot>> GetSpotsAsync(string locationId)
    {
        lock (_lock)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var bookedIds = _bookings
                .Where(b => b.BookingDate == today && b.Status == BookingStatus.Active)
                .Select(b => b.DeskId)
                .ToHashSet();

            var result = _spots
                .Where(s => s.LocationId == locationId)
                .Select(s => s with { Status = bookedIds.Contains(s.DeskId) ? ResourceStatus.Occupied : s.Status })
                .AsEnumerable();
            return Task.FromResult(result);
        }
    }

    public Task<DeskSpot?> GetSpotByIdAsync(string deskId)
    {
        lock (_lock) { return Task.FromResult(_spots.FirstOrDefault(s => s.DeskId == deskId)); }
    }

    public Task<IEnumerable<DeskBooking>> GetBookingsByDateAsync(string locationId, DateOnly date)
    {
        lock (_lock)
        {
            var deskIds = _spots.Where(s => s.LocationId == locationId).Select(s => s.DeskId).ToHashSet();
            return Task.FromResult(_bookings.Where(b => b.BookingDate == date && deskIds.Contains(b.DeskId)).AsEnumerable());
        }
    }

    public Task<DeskBooking?> GetActiveBookingAsync(string userId, DateOnly date)
    {
        lock (_lock)
        {
            return Task.FromResult(_bookings.FirstOrDefault(b =>
                b.UserId == userId && b.BookingDate == date && b.Status == BookingStatus.Active));
        }
    }

    public Task<DeskBooking> CreateBookingAsync(DeskBooking booking)
    {
        lock (_lock)
        {
            _bookings.Add(booking);
            var spot = _spots.FirstOrDefault(s => s.DeskId == booking.DeskId);
            if (spot is not null) spot.Status = ResourceStatus.Occupied;
            return Task.FromResult(booking);
        }
    }

    public Task<bool> CancelBookingAsync(string bookingId, string userId)
    {
        lock (_lock)
        {
            var booking = _bookings.FirstOrDefault(b => b.BookingId == bookingId && b.UserId == userId);
            if (booking is null) return Task.FromResult(false);
            booking.Status = BookingStatus.Cancelled;
            var spot = _spots.FirstOrDefault(s => s.DeskId == booking.DeskId);
            if (spot is not null) spot.Status = ResourceStatus.Available;
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryAcquireLockAsync(string deskId, string userId, TimeSpan lockDuration)
    {
        lock (_lock)
        {
            var spot = _spots.FirstOrDefault(s => s.DeskId == deskId);
            if (spot is null || spot.Status != ResourceStatus.Available) return Task.FromResult(false);
            var existingLock = _bookings.FirstOrDefault(b =>
                b.DeskId == deskId &&
                b.LockedUntil.HasValue &&
                b.LockedUntil > DateTime.UtcNow &&
                b.LockedByUserId != userId);
            if (existingLock is not null) return Task.FromResult(false);
            spot.Status = ResourceStatus.Pending;
            _bookings.Add(new DeskBooking
            {
                DeskId = deskId,
                UserId = userId,
                BookingDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = BookingStatus.Active,
                LockedUntil = DateTime.UtcNow.Add(lockDuration),
                LockedByUserId = userId,
            });
            return Task.FromResult(true);
        }
    }

    public Task ReleaseExpiredLocksAsync()
    {
        lock (_lock)
        {
            var expired = _bookings.Where(b => b.LockedUntil.HasValue && b.LockedUntil < DateTime.UtcNow).ToList();
            foreach (var b in expired)
            {
                b.LockedUntil = null;
                b.LockedByUserId = null;
                b.Status = BookingStatus.Cancelled;
                var spot = _spots.FirstOrDefault(s => s.DeskId == b.DeskId);
                if (spot?.Status == ResourceStatus.Pending) spot.Status = ResourceStatus.Available;
            }
        }
        return Task.CompletedTask;
    }
}
