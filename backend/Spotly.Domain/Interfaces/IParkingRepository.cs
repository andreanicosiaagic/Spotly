using Spotly.Domain.Entities;

namespace Spotly.Domain.Interfaces;

public interface IParkingRepository
{
    Task<IEnumerable<ParkingSpot>> GetSpotsAsync(string locationId);
    Task<ParkingSpot?> GetSpotByIdAsync(string spotId);
    Task<IEnumerable<ParkingBooking>> GetBookingsByDateAsync(string locationId, DateOnly date);
    Task<ParkingBooking?> GetActiveBookingAsync(string userId, DateOnly date);
    Task<ParkingBooking> CreateBookingAsync(ParkingBooking booking);
    Task<bool> CancelBookingAsync(string bookingId, string userId);
    Task<bool> TryAcquireLockAsync(string spotId, string userId, TimeSpan lockDuration);
    Task ReleaseExpiredLocksAsync();
}
