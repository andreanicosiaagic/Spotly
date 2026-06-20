using Spotly.Domain.Entities;

namespace Spotly.Domain.Interfaces;

public interface IDeskRepository
{
    Task<IEnumerable<DeskSpot>> GetSpotsAsync(string locationId);
    Task<DeskSpot?> GetSpotByIdAsync(string deskId);
    Task<IEnumerable<DeskBooking>> GetBookingsByDateAsync(string locationId, DateOnly date);
    Task<DeskBooking?> GetActiveBookingAsync(string userId, DateOnly date);
    Task<DeskBooking> CreateBookingAsync(DeskBooking booking);
    Task<bool> CancelBookingAsync(string bookingId, string userId);
    Task<bool> TryAcquireLockAsync(string deskId, string userId, TimeSpan lockDuration);
    Task ReleaseExpiredLocksAsync();
}
