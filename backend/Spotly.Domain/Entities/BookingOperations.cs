namespace Spotly.Domain.Entities;

public enum BookingFailure
{
    None,
    NotFound,
    AlreadyBooked,
    ResourceUnavailable,
    Ineligible,
}

public record BookingAttempt<T>(T? Booking, BookingFailure Failure) where T : class
{
    public bool Succeeded => Booking is not null && Failure == BookingFailure.None;
}

public record CancellationOutcome(
    bool Found,
    BookingStatus? Status = null,
    string? ResourceId = null,
    DateOnly? BookingDate = null);

public record ReleasedResource(string ResourceId, DateOnly BookingDate);
