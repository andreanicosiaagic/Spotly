namespace Spotly.Api.Endpoints;

public static class MeEndpoints
{
    public static void MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/me", (HttpContext ctx) =>
        {
            // Easy Auth populates HttpContext.User from X-MS-CLIENT-PRINCIPAL header.
            // In local dev, a middleware simulates this from X-Dev-User header.
            var user = ctx.User;
            var userId = user.FindFirst("oid")?.Value
                      ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
                      ?? "anonymous";
            var name = user.FindFirst("name")?.Value
                    ?? user.FindFirst("preferred_username")?.Value
                    ?? "Unknown";
            var roles = user.FindAll("roles").Select(c => c.Value).ToArray();

            return Results.Ok(new { userId, name, roles });
        }).WithTags("Auth");
    }
}
