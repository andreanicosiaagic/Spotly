using Spotly.Domain.Entities;

namespace Spotly.Domain.Interfaces;

public interface ILunchRepository
{
    Task<IEnumerable<Restaurant>> GetRestaurantsAsync(string locationId);
    Task<IEnumerable<RestaurantSlot>> GetSlotsByDateAsync(string locationId, DateOnly date, string? restaurantId = null);
    Task<IEnumerable<MenuItem>> GetMenuAsync(DateOnly date, string? restaurantId = null);
    Task<IEnumerable<LunchBoxCatalog>> GetLunchBoxesAsync();
    Task<LunchBooking?> GetActiveBookingAsync(string userId, DateOnly date);
    Task<LunchBooking> CreateBookingAsync(LunchBooking booking);
    Task<bool> CancelBookingAsync(string bookingId, string userId);
    Task<bool> TryDecrementSlotAsync(string slotId);
}
