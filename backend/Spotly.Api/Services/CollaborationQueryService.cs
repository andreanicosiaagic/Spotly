using System.Security.Claims;
using Spotly.Api.Auth;
using Spotly.Api.Endpoints;
using Spotly.Domain.Entities;
using Spotly.Domain.Interfaces;
using Spotly.Domain.Rules;

namespace Spotly.Api.Services;

public sealed class CollaborationQueryService(ICollaborationAvailabilityProvider provider, OfficeTime officeTime)
{
    public async Task<IResult> GetMyAvailabilityAsync(ClaimsPrincipal user, string? date, string? from, string? to)
    {
        if (!TryWindow(date, from, to, out var selectedDate, out var fromUtc, out var toUtc, out var error)) return error!;
        var availability = await provider.GetUserAvailabilityAsync(CurrentUser.Id(user), selectedDate, fromUtc, toUtc);
        return availability is null ? Results.NotFound() : Results.Ok(availability);
    }

    public async Task<IResult> GetTeamMatchAsync(ClaimsPrincipal user, string? date, string? from, string? to)
    {
        if (!TryWindow(date, from, to, out var selectedDate, out var fromUtc, out var toUtc, out var error)) return error!;
        var userId = CurrentUser.Id(user);
        var current = await provider.GetUserAvailabilityAsync(userId, selectedDate, fromUtc, toUtc);
        if (current is null) return EndpointSupport.Problem(StatusCodes.Status404NotFound, "TEAMS_LOCATION_UNAVAILABLE", "Work location Teams non disponibile per l'utente corrente.");
        var members = await provider.GetTeamAvailabilityAsync(userId, selectedDate, fromUtc, toUtc);
        return Results.Ok(TeamAvailabilityMatcher.Match(selectedDate, fromUtc, toUtc, current, members));
    }

    private bool TryWindow(string? date, string? from, string? to, out DateOnly selectedDate, out TimeOnly fromUtc, out TimeOnly toUtc, out IResult? error)
    {
        fromUtc = new TimeOnly(9, 0);
        toUtc = new TimeOnly(17, 0);
        var valid = EndpointSupport.TryDate(date, officeTime.Today, out selectedDate)
            && (string.IsNullOrWhiteSpace(from) ? Assign(new TimeOnly(9, 0), out fromUtc) : TimeOnly.TryParse(from, out fromUtc))
            && (string.IsNullOrWhiteSpace(to) ? Assign(new TimeOnly(17, 0), out toUtc) : TimeOnly.TryParse(to, out toUtc))
            && fromUtc < toUtc;
        error = valid ? null : Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["window"] = ["Use a valid date and a time window where from is before to."],
        });
        return valid;
    }

    private static bool Assign(TimeOnly value, out TimeOnly target)
    {
        target = value;
        return true;
    }
}
