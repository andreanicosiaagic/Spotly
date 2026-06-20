using Spotly.Api.Auth;
using Spotly.Domain.Entities;
using Spotly.Domain.Interfaces;
using Spotly.Domain.Rules;

namespace Spotly.Api.Endpoints;

public static class CollaborationEndpoints
{
    public static void MapCollaborationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/collaboration").WithTags("Collaboration").RequireAuthorization("Employee");

        group.MapGet("/me", async (HttpContext context, ICollaborationAvailabilityProvider provider, TimeProvider clock,
            string? date, string? from, string? to) =>
        {
            if (!TryWindow(clock, date, from, to, out var selectedDate, out var fromUtc, out var toUtc, out var error)) return error!;
            var availability = await provider.GetUserAvailabilityAsync(CurrentUser.Id(context.User), selectedDate, fromUtc, toUtc);
            return availability is null ? Results.NotFound() : Results.Ok(availability);
        });

        group.MapGet("/team-match", async (HttpContext context, ICollaborationAvailabilityProvider provider, TimeProvider clock,
            string? date, string? from, string? to) =>
        {
            if (!TryWindow(clock, date, from, to, out var selectedDate, out var fromUtc, out var toUtc, out var error)) return error!;
            var userId = CurrentUser.Id(context.User);
            var current = await provider.GetUserAvailabilityAsync(userId, selectedDate, fromUtc, toUtc);
            if (current is null) return Results.NotFound(new { error = "Work location Teams non disponibile per l'utente corrente." });
            var members = await provider.GetTeamAvailabilityAsync(userId, selectedDate, fromUtc, toUtc);
            return Results.Ok(TeamAvailabilityMatcher.Match(selectedDate, fromUtc, toUtc, current, members));
        }).RequireAuthorization("Manager");
    }

    private static bool TryWindow(TimeProvider clock, string? date, string? from, string? to, out DateOnly selectedDate,
        out TimeOnly fromUtc, out TimeOnly toUtc, out IResult? error)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        fromUtc = new TimeOnly(9, 0);
        toUtc = new TimeOnly(17, 0);
        var valid = EndpointSupport.TryDate(date, today, out selectedDate)
            && (string.IsNullOrWhiteSpace(from) ? Assign(new TimeOnly(9, 0), out fromUtc) : TimeOnly.TryParse(from, out fromUtc))
            && (string.IsNullOrWhiteSpace(to) ? Assign(new TimeOnly(17, 0), out toUtc) : TimeOnly.TryParse(to, out toUtc))
            && fromUtc < toUtc;
        error = valid ? null : Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["window"] = ["Use a valid date and a time window where from is before to."],
        });
        return valid;
    }

    private static bool Assign(TimeOnly value, out TimeOnly target) { target = value; return true; }
}
