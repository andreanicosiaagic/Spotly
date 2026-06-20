using System.Globalization;
using Spotly.Domain.Entities;

namespace Spotly.Api.Endpoints;

internal static class EndpointSupport
{
    public static IResult BookingFailureResult(BookingFailure failure, string resourceType) => failure switch
    {
        BookingFailure.NotFound => Problem(StatusCodes.Status404NotFound, "RESOURCE_NOT_FOUND", "Risorsa non trovata."),
        BookingFailure.AlreadyBooked => Problem(StatusCodes.Status409Conflict, "R01_ALREADY_BOOKED",
            $"R-01: hai gia' una prenotazione {resourceType} attiva per questo giorno."),
        BookingFailure.Ineligible => Problem(StatusCodes.Status403Forbidden, "R06_INELIGIBLE",
            "La quota o l'idoneita' richiesta prevale sulla scelta individuale."),
        _ => Problem(StatusCodes.Status409Conflict, "R03_RESOURCE_UNAVAILABLE", "R-03: risorsa non disponibile o gia' bloccata."),
    };

    public static bool TryDate(string? value, DateOnly fallback, out DateOnly date) =>
        string.IsNullOrWhiteSpace(value)
            ? Assign(fallback, out date)
            : DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    public static IResult InvalidDate(string field = "date") => Results.ValidationProblem(new Dictionary<string, string[]>
    {
        [field] = ["Use yyyy-MM-dd."],
    });

    public static IResult Problem(int statusCode, string code, string detail, string? title = null) => Results.Problem(
        title: title ?? code,
        detail: detail,
        statusCode: statusCode,
        extensions: new Dictionary<string, object?> { ["code"] = code });

    private static bool Assign(DateOnly value, out DateOnly target)
    {
        target = value;
        return true;
    }
}
