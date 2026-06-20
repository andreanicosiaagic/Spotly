using Spotly.Domain.Entities;
using Spotly.Domain.Interfaces;
using Spotly.Infrastructure.Seed;

namespace Spotly.Infrastructure.Repositories;

public class InMemoryLunchRepository : ILunchRepository
{
    private readonly List<Restaurant> _restaurants = SeedData.Restaurants();
    private readonly List<RestaurantSlot> _slots = SeedData.RestaurantSlots(DateOnly.FromDateTime(DateTime.UtcNow));
    private readonly List<MenuItem> _menuItems = SeedData.MenuItems(DateOnly.FromDateTime(DateTime.UtcNow));
    private readonly List<LunchBoxCatalog> _lunchBoxes = SeedData.LunchBoxes();
    private readonly List<LunchBooking> _bookings = [];
    private readonly Lock _lock = new();

    public Task<IEnumerable<Restaurant>> GetRestaurantsAsync(string locationId) =>
        Task.FromResult(_restaurants.Where(r => r.LocationId == locationId).AsEnumerable());

    public Task<IEnumerable<RestaurantSlot>> GetSlotsByDateAsync(string locationId, DateOnly date, string? restaurantId = null)
    {
        var restaurantIds = _restaurants.Where(r => r.LocationId == locationId).Select(r => r.RestaurantId).ToHashSet();
        var result = _slots
            .Where(s => s.BookingDate == date && restaurantIds.Contains(s.RestaurantId))
            .Where(s => restaurantId == null || s.RestaurantId == restaurantId)
            .AsEnumerable();
        return Task.FromResult(result);
    }

    public Task<IEnumerable<MenuItem>> GetMenuAsync(DateOnly date, string? restaurantId = null)
    {
        var result = _menuItems
            .Where(m => m.MenuDate == date)
            .Where(m => restaurantId == null || m.RestaurantId == restaurantId)
            .AsEnumerable();
        return Task.FromResult(result);
    }

    public Task<IEnumerable<LunchBoxCatalog>> GetLunchBoxesAsync() =>
        Task.FromResult(_lunchBoxes.Where(lb => lb.IsAvailable).AsEnumerable());

    public Task<LunchBooking?> GetActiveBookingAsync(string userId, DateOnly date)
    {
        lock (_lock)
        {
            return Task.FromResult(_bookings.FirstOrDefault(b =>
                b.UserId == userId && b.BookingDate == date && b.Status == BookingStatus.Active));
        }
    }

    public Task<LunchBooking> CreateBookingAsync(LunchBooking booking)
    {
        lock (_lock)
        {
            _bookings.Add(booking);
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
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryDecrementSlotAsync(string slotId)
    {
        lock (_lock)
        {
            var slot = _slots.FirstOrDefault(s => s.SlotId == slotId);
            if (slot is null || slot.Available <= 0) return Task.FromResult(false);
            slot.Available -= 1;
            return Task.FromResult(true);
        }
    }
}
