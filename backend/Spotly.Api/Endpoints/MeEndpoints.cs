using System.Security.Claims;
using Spotly.Api.Auth;

namespace Spotly.Api.Endpoints;

public static class MeEndpoints
{
    public static void MapMeEndpoints(this IEndpointRouteBuilder app) => app.MapGet("/api/me", (HttpContext context) => Results.Ok(new
    {
        userId = CurrentUser.Id(context.User),
        name = context.User.Identity?.Name ?? "Mock user",
        roles = context.User.FindAll(ClaimTypes.Role).Select(x => x.Value).ToArray(),
        department = CurrentUser.Department(context.User),
    })).WithTags("Auth").RequireAuthorization("Employee");
}
