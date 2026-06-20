using System.Security.Claims;
using Spotly.Api.Services;

namespace Spotly.Api.Endpoints;

public static class MeEndpoints
{
    public static void MapMeEndpoints(this IEndpointRouteBuilder app) => app.MapGet("/api/me",
        (HttpContext context, UserProfileService service) => service.GetMe(context.User))
        .WithTags("Auth")
        .RequireAuthorization("Employee");
}
