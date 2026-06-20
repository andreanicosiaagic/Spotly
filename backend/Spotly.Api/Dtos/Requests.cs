namespace Spotly.Api.Dtos;

// Requests
public record CreateParkingBookingRequest(string SpotId, string BookingDate, string UserId);
public record CreateDeskBookingRequest(string DeskId, string BookingDate, string UserId);
public record CreateLunchBookingRequest(
    string BookingDate,
    string UserId,
    bool IsLunchBox,
    string? RestaurantId = null,
    string? SlotId = null,
    string? LunchBoxId = null,
    string? Allergens = null);

// Availability (SignalR payload)
public record AvailabilityUpdate(string ResourceId, string ResourceType, string NewStatus);
