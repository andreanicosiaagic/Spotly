namespace Spotly.Domain.Entities;

public record class DeskSpot
{
    public string DeskId { get; set; } = string.Empty;
    public string LocationId { get; set; } = string.Empty;
    public int Floor { get; set; }
    public string Zone { get; set; } = string.Empty;
    public bool HasMonitor { get; set; }
    public bool IsStanding { get; set; }
    public bool HasWindow { get; set; }
    public ResourceStatus Status { get; set; } = ResourceStatus.Available;
}

public class DeskBooking
{
    public string BookingId { get; set; } = Guid.NewGuid().ToString();
    public string DeskId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateOnly BookingDate { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Active;
    public DateTime? LockedUntil { get; set; }
    public string? LockedByUserId { get; set; }
}
