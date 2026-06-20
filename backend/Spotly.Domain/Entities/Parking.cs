namespace Spotly.Domain.Entities;

public record class ParkingSpot
{
    public string SpotId { get; set; } = string.Empty;
    public string LocationId { get; set; } = string.Empty;
    public int Level { get; set; }
    public string SpotNumber { get; set; } = string.Empty;
    public ParkingSpotType Type { get; set; } = ParkingSpotType.Standard;
    public ResourceStatus Status { get; set; } = ResourceStatus.Available;
}

public class ParkingBooking
{
    public string BookingId { get; set; } = Guid.NewGuid().ToString();
    public string SpotId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateOnly BookingDate { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Active;
    public DateTime? LockedUntil { get; set; }
    public string? LockedByUserId { get; set; }
    public DateTime? CheckedInAtUtc { get; set; }
    public DateTime CheckInDeadlineUtc { get; set; }
}
