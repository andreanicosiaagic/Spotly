using Spotly.Domain.Entities;

namespace Spotly.Domain.Interfaces;

public interface IParkingRepository
{
    Task<IEnumerable<ParkingSpot>> GetSpotsAsync(string locationId, DateOnly date);
    Task<ParkingSpot?> GetSpotByIdAsync(string spotId);
    Task<IEnumerable<ParkingBooking>> GetBookingsByDateAsync(string locationId, DateOnly date);
    Task<ParkingBooking?> GetActiveBookingAsync(string userId, DateOnly date);
    Task<BookingAttempt<ParkingBooking>> TryCreateBookingAsync(ParkingBooking booking);
    Task<CancellationOutcome> CancelBookingAsync(string bookingId, string userId, DateTime utcNow);
    Task<bool> CheckInAsync(string bookingId, string userId, DateTime utcNow);
    Task<bool> TryAcquireLockAsync(string spotId, string userId, DateOnly bookingDate, TimeSpan lockDuration);
    Task<IReadOnlyList<ReleasedResource>> ReleaseExpiredLocksAsync(DateTime utcNow);
    Task<IReadOnlyList<ReleasedResource>> ReleaseNoShowsAsync(DateTime utcNow);
}
