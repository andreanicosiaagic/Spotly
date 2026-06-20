using Spotly.Domain.Entities;

namespace Spotly.Domain.Interfaces;

public interface IDeskRepository
{
    Task<IEnumerable<DeskSpot>> GetSpotsAsync(string locationId, DateOnly date);
    Task<DeskSpot?> GetSpotByIdAsync(string deskId);
    Task<IEnumerable<DeskBooking>> GetBookingsByDateAsync(string locationId, DateOnly date);
    Task<DeskBooking?> GetActiveBookingAsync(string userId, DateOnly date);
    Task<BookingAttempt<DeskBooking>> TryCreateBookingAsync(DeskBooking booking, string? department);
    Task<CancellationOutcome> CancelBookingAsync(string bookingId, string userId, DateTime utcNow);
    Task<bool> CheckInAsync(string bookingId, string userId, DateTime utcNow);
    Task<bool> TryAcquireLockAsync(string deskId, string userId, TimeSpan lockDuration);
    Task<IReadOnlyList<ReleasedResource>> ReleaseExpiredLocksAsync(DateTime utcNow);
    Task<IReadOnlyList<ReleasedResource>> ReleaseNoShowsAsync(DateTime utcNow);
}
