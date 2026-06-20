using Spotly.Domain.Entities;

namespace Spotly.Api.Endpoints;

internal static class EndpointSupport
{
    public static IResult BookingFailureResult(BookingFailure failure, string resourceType) => failure switch
    {
        BookingFailure.NotFound => Results.NotFound(new { error = "Risorsa non trovata." }),
        BookingFailure.AlreadyBooked => Results.Conflict(new { error = $"R-01: hai già una prenotazione {resourceType} attiva per questo giorno." }),
        BookingFailure.Ineligible => Results.Json(new { error = "La quota o l'idoneità richiesta prevale sulla scelta individuale." }, statusCode: StatusCodes.Status403Forbidden),
        _ => Results.Conflict(new { error = "R-03: risorsa non disponibile o già bloccata." }),
    };

    public static bool TryDate(string? value, DateOnly fallback, out DateOnly date) =>
        string.IsNullOrWhiteSpace(value) ? Assign(fallback, out date) : DateOnly.TryParse(value, out date);

    public static DateTime CheckInDeadline(DateOnly date, IConfiguration configuration)
    {
        var cutoff = TimeOnly.TryParse(configuration["Booking:CheckInCutoffUtc"], out var parsed) ? parsed : new TimeOnly(9, 30);
        return DateTime.SpecifyKind(date.ToDateTime(cutoff), DateTimeKind.Utc);
    }

    private static bool Assign(DateOnly value, out DateOnly target) { target = value; return true; }
}
