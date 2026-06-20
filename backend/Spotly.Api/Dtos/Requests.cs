using Spotly.Domain.Entities;

namespace Spotly.Api.Dtos;

public record CreateParkingBookingRequest(string SpotId, string BookingDate);
public record CreateDeskBookingRequest(string DeskId, string BookingDate);
public record CreateLunchBookingRequest(
    string BookingDate,
    bool IsLunchBox,
    string? RestaurantId = null,
    string? SlotId = null,
    IReadOnlyList<string>? MenuItemIds = null,
    string? LunchBoxId = null,
    string? Allergens = null);
public record AvailabilityUpdate(string ResourceId, string ResourceType, string NewStatus);
public record CancellationResponse(string Status);
public record LockAcquisitionResponse(DateTime LockedUntilUtc, string BookingDate);
public record RestaurantAvailabilityUpdate(
    string LocationId,
    string BookingDate,
    string RestaurantId,
    string Name,
    int Capacity,
    int AvailableSeats,
    long Sequence,
    DateTime UpdatedAtUtc,
    string Source);
public record RestaurantMessageEvent(
    string LocationId,
    string BookingDate,
    string RestaurantId,
    string Kind,
    string Outcome,
    long Sequence,
    DateTime ReceivedAtUtc);
public record SimulateRestaurantAvailabilityRequest(int AvailableSeats);
public record SimulateBookingOutcomeRequest(string Code);
public record RestaurantBookingResponse(LunchBooking Booking, string PartnerCode, int AvailableSeats, string? PartnerReference);
public record LunchBoxEligibilityResponse(
    bool Eligible,
    string Reason,
    string CutoffLocal,
    bool RestaurantsFull,
    bool OutsideOperatingHours,
    string DemoDate);
