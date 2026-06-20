namespace Spotly.Domain.Entities;

public class Restaurant
{
    public string RestaurantId { get; set; } = string.Empty;
    public string LocationId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string? WhatsAppNumber { get; set; }
    public string? TelegramChatId { get; set; }
    public bool IsActive { get; set; } = true;
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
    public PartnerBookingStatus PartnerStatus { get; set; } = PartnerBookingStatus.NotRequired;
    public string? PartnerCode { get; set; }
    public string? PartnerReference { get; set; }
    public string? PartnerCorrelationId { get; set; }
    public int? PartnerAvailableSeats { get; set; }
    public DateTime? PartnerRespondedAtUtc { get; set; }
}

public class RestaurantAvailability
{
    public string RestaurantId { get; set; } = string.Empty;
    public DateOnly BookingDate { get; set; }
    public int AvailableSeats { get; set; }
    public int PendingSeats { get; set; }
    public long Sequence { get; set; }
    public string LastMessageId { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
    public string Source { get; set; } = "telegram-mock";
}

public class RestaurantPartnerMessage
{
    public string MessageId { get; set; } = string.Empty;
    public string RestaurantId { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public long Sequence { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public PartnerMessageOutcome Outcome { get; set; }
    public string Payload { get; set; } = string.Empty;
}

public record RestaurantAvailabilityView(
    string RestaurantId,
    string Name,
    int Capacity,
    int AvailableSeats,
    long Sequence,
    DateTime UpdatedAtUtc,
    bool PartnerChannelConfigured);

public record PartnerAvailabilityMessage(
    string MessageId,
    string RestaurantId,
    DateOnly BookingDate,
    int AvailableSeats,
    long Sequence,
    DateTime SentAtUtc);

public record PartnerBookingCommand(
    string CorrelationId,
    string RestaurantId,
    DateOnly BookingDate,
    int PartySize,
    int AvailableBeforeConfirmation);

public record PartnerBookingResult(
    string CorrelationId,
    string RestaurantId,
    DateOnly BookingDate,
    string Code,
    int RemainingSeats,
    string? PartnerReference)
{
    public bool Confirmed => Code == "OK";
}

public record RestaurantBookingStart(BookingAttempt<LunchBooking> Attempt, int AvailableBeforeConfirmation);
public record RestaurantMessageApplyResult(PartnerMessageOutcome Outcome, RestaurantAvailabilityView? Availability = null);
