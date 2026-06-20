using Spotly.Domain.Entities;

namespace Spotly.Domain.Interfaces;

public interface ILunchRepository
{
    Task<IEnumerable<Restaurant>> GetRestaurantsAsync(string locationId);
    Task<IReadOnlyList<RestaurantAvailabilityView>> GetRestaurantAvailabilityAsync(string locationId, DateOnly date);
    Task<RestaurantMessageApplyResult> ApplyAvailabilityAsync(PartnerAvailabilityMessage message, string payload, DateTime receivedAtUtc);
    Task<RestaurantBookingStart> BeginRestaurantBookingAsync(LunchBooking booking);
    Task<BookingAttempt<LunchBooking>> CompleteRestaurantBookingAsync(string correlationId, PartnerBookingResult result, DateTime respondedAtUtc);
    Task<IReadOnlyList<RestaurantPartnerMessage>> GetRecentPartnerMessagesAsync(DateOnly? date, int take);
    Task<IEnumerable<RestaurantSlot>> GetSlotsByDateAsync(string locationId, DateOnly date, string? restaurantId = null);
    Task<IEnumerable<MenuItem>> GetMenuAsync(DateOnly date, string? restaurantId = null);
    Task<IEnumerable<LunchBoxCatalog>> GetLunchBoxesAsync();
    Task<LunchBooking?> GetActiveBookingAsync(string userId, DateOnly date);
    Task<LunchBooking?> GetUserBookingAsync(string bookingId, string userId);
    Task<BookingAttempt<LunchBooking>> TryCreateBookingAsync(LunchBooking booking, bool isOutsideHours);
    Task<CancellationOutcome> CancelBookingAsync(string bookingId, string userId, DateTime utcNow);
    Task<IReadOnlyList<ReleasedResource>> ReleaseExpiredPendingPartnerBookingsAsync(DateTime utcNow);
}
