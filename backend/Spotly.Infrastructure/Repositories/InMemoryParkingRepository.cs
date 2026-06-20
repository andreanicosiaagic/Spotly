using Spotly.Domain.Entities;
using Spotly.Domain.Interfaces;
using Spotly.Infrastructure.Seed;

namespace Spotly.Infrastructure.Repositories;

public class InMemoryParkingRepository : IParkingRepository
{
    private readonly List<ParkingSpot> _spots = SeedData.ParkingSpots();
    private readonly List<ParkingBooking> _bookings = [];
    private readonly Lock _lock = new();

    public Task<IEnumerable<ParkingSpot>> GetSpotsAsync(string locationId)
    {
        lock (_lock)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var bookedIds = _bookings
                .Where(b => b.BookingDate == today && b.Status == BookingStatus.Active)
                .Select(b => b.SpotId)
                .ToHashSet();

            var result = _spots
                .Where(s => s.LocationId == locationId)
                .Select(s => s with { Status = bookedIds.Contains(s.SpotId) ? ResourceStatus.Occupied : s.Status })
                .AsEnumerable();
            return Task.FromResult(result);
        }
    }

    public Task<ParkingSpot?> GetSpotByIdAsync(string spotId)
    {
        lock (_lock) { return Task.FromResult(_spots.FirstOrDefault(s => s.SpotId == spotId)); }
    }

    public Task<IEnumerable<ParkingBooking>> GetBookingsByDateAsync(string locationId, DateOnly date)
    {
        lock (_lock)
        {
            var bookedSpotIds = _spots.Where(s => s.LocationId == locationId).Select(s => s.SpotId).ToHashSet();
            return Task.FromResult(_bookings.Where(b => b.BookingDate == date && bookedSpotIds.Contains(b.SpotId)).AsEnumerable());
        }
    }

    public Task<ParkingBooking?> GetActiveBookingAsync(string userId, DateOnly date)
    {
        lock (_lock)
        {
            return Task.FromResult(_bookings.FirstOrDefault(b =>
                b.UserId == userId && b.BookingDate == date && b.Status == BookingStatus.Active));
        }
    }

    public Task<ParkingBooking> CreateBookingAsync(ParkingBooking booking)
    {
        lock (_lock)
        {
            _bookings.Add(booking);
            var spot = _spots.FirstOrDefault(s => s.SpotId == booking.SpotId);
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
            var spot = _spots.FirstOrDefault(s => s.SpotId == booking.SpotId);
            if (spot is not null) spot.Status = ResourceStatus.Available;
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryAcquireLockAsync(string spotId, string userId, TimeSpan lockDuration)
    {
        lock (_lock)
        {
            var spot = _spots.FirstOrDefault(s => s.SpotId == spotId);
            if (spot is null || spot.Status != ResourceStatus.Available) return Task.FromResult(false);
            // Check no unexpired lock exists from another user
            var existingLock = _bookings.FirstOrDefault(b =>
                b.SpotId == spotId &&
                b.LockedUntil.HasValue &&
                b.LockedUntil > DateTime.UtcNow &&
                b.LockedByUserId != userId);
            if (existingLock is not null) return Task.FromResult(false);
            spot.Status = ResourceStatus.Pending;
            var lockEntry = new ParkingBooking
            {
                SpotId = spotId,
                UserId = userId,
                BookingDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = BookingStatus.Active,
                LockedUntil = DateTime.UtcNow.Add(lockDuration),
                LockedByUserId = userId,
            };
            _bookings.Add(lockEntry);
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
                var spot = _spots.FirstOrDefault(s => s.SpotId == b.SpotId);
                if (spot?.Status == ResourceStatus.Pending) spot.Status = ResourceStatus.Available;
            }
        }
        return Task.CompletedTask;
    }
}
