namespace Spotly.Domain.Entities;

public class Restaurant
{
    public string RestaurantId { get; set; } = string.Empty;
    public string LocationId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
}

public class RestaurantSlot
{
    public string SlotId { get; set; } = string.Empty;
    public string RestaurantId { get; set; } = string.Empty;
    public TimeOnly SlotTime { get; set; }
    public int Capacity { get; set; }
    public int Available { get; set; }
    public DateOnly BookingDate { get; set; }
}

public class MenuItem
{
    public string ItemId { get; set; } = Guid.NewGuid().ToString();
    public string RestaurantId { get; set; } = string.Empty;
    public DateOnly MenuDate { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // primo|secondo|contorno|dessert
    public string? Allergens { get; set; }
}

public class LunchBoxCatalog
{
    public string BoxId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Allergens { get; set; }
    public bool IsAvailable { get; set; } = true;
}

public class LunchBooking
{
    public string BookingId { get; set; } = Guid.NewGuid().ToString();
    public string? RestaurantId { get; set; }
    public string? SlotId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateOnly BookingDate { get; set; }
    public bool IsLunchBox { get; set; }
    public string? LunchBoxId { get; set; }
    public string? Allergens { get; set; }
    public DeliveryStatus DeliveryStatus { get; set; } = DeliveryStatus.Pending;
    public BookingStatus Status { get; set; } = BookingStatus.Active;
}
