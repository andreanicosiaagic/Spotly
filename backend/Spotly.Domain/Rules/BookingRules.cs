namespace Spotly.Domain.Rules;

/// <summary>
/// Business rules R-01..R-09 from the functional analysis.
/// These live in Domain and are independent of infrastructure.
/// </summary>
public static class BookingRules
{
    /// <summary>R-02: Booking window — max 14 days ahead.</summary>
    public const int MaxBookingWindowDays = 14;

    /// <summary>R-03: Optimistic lock duration (minutes).</summary>
    public const int LockDurationMinutes = 3;

    /// <summary>R-09: Cancellation threshold — free cancellation before this hour on the booking day.</summary>
    public const int FreeCancellationCutoffHour = 8;

    /// <summary>
    /// R-01: A user may have at most one active booking per resource type per day.
    /// Returns an error message if violated, null if ok.
    /// </summary>
    public static string? ValidateSingleBookingPerDay(bool hasExistingBooking, string resourceType) =>
        hasExistingBooking
            ? $"R-01: hai già una prenotazione {resourceType} attiva per questo giorno."
            : null;

    /// <summary>
    /// R-02: Booking must be within the allowed window (today → today + 14 days).
    /// Returns an error message if violated, null if ok.
    /// </summary>
    public static string? ValidateBookingWindow(DateOnly bookingDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (bookingDate < today)
            return "R-02: non puoi prenotare per una data passata.";
        if (bookingDate > today.AddDays(MaxBookingWindowDays))
            return $"R-02: puoi prenotare al massimo {MaxBookingWindowDays} giorni in anticipo.";
        return null;
    }

    /// <summary>
    /// R-02 (lunch): lunch box cutoff is 23:59 of the previous day.
    /// Returns an error message if violated, null if ok.
    /// </summary>
    public static string? ValidateLunchBookingWindow(DateOnly bookingDate, bool isLunchBox, DateTime utcNow)
    {
        var baseError = ValidateBookingWindow(bookingDate, utcNow);
        if (baseError is not null) return baseError;
        var today = DateOnly.FromDateTime(utcNow);
        return isLunchBox && bookingDate <= today
            ? "R-02: il lunch box deve essere prenotato entro le 23:59 del giorno precedente."
            : null;
    }

    /// <summary>
    /// R-06: Lunch box is available only when the restaurant is full or outside operating hours.
    /// Returns an error message if violated, null if ok.
    /// </summary>
    public static string? ValidateLunchBoxEligibility(bool restaurantHasAvailability, bool isOutsideHours) =>
        !restaurantHasAvailability || isOutsideHours
            ? null
            : "R-06: il lunch box è disponibile solo quando i locali sono al completo o fuori orario.";

    /// <summary>
    /// R-09: Returns true if cancellation is free (before cutoff on booking day).
    /// </summary>
    public static bool IsFreeCancellation(DateOnly bookingDate) => IsFreeCancellation(bookingDate, DateTime.UtcNow);

    public static bool IsFreeCancellation(DateOnly bookingDate, DateTime utcNow)
    {
        var today = DateOnly.FromDateTime(utcNow);
        return bookingDate > today || (bookingDate == today && utcNow.Hour < FreeCancellationCutoffHour);
    }

    public static string? ValidateBookingWindow(DateOnly bookingDate, DateTime utcNow)
    {
        var today = DateOnly.FromDateTime(utcNow);
        if (bookingDate < today) return "R-02: non puoi prenotare per una data passata.";
        if (bookingDate > today.AddDays(MaxBookingWindowDays))
            return $"R-02: puoi prenotare al massimo {MaxBookingWindowDays} giorni in anticipo.";
        return null;
    }
}
