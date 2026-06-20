using Spotly.Api.Auth;
using Spotly.Api.Services;

namespace Spotly.Api.Endpoints;

public static class CollaborationEndpoints
{
    public static void MapCollaborationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/collaboration").WithTags("Collaboration").RequireAuthorization("Employee");

        group.MapGet("/me", (HttpContext context, CollaborationQueryService service, string? date, string? from, string? to) =>
            service.GetMyAvailabilityAsync(context.User, date, from, to));

        group.MapGet("/team-match", (HttpContext context, CollaborationQueryService service, string? date, string? from, string? to) =>
            service.GetTeamMatchAsync(context.User, date, from, to)).RequireAuthorization("Manager");
    }
}
