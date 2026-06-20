using System.Security.Claims;
using Spotly.Api.Auth;

namespace Spotly.Api.Services;

public sealed class UserProfileService
{
    public IResult GetMe(ClaimsPrincipal user) => Results.Ok(new
    {
        oid = CurrentUser.Id(user),
        userId = CurrentUser.Id(user),
        name = user.Identity?.Name ?? "Mock user",
        email = user.FindFirst("preferred_username")?.Value ?? $"{CurrentUser.Id(user)}@spotly.test",
        roles = user.FindAll(ClaimTypes.Role).Select(x => x.Value).ToArray(),
        department = CurrentUser.Department(user),
        parkingEligibility = user.FindAll("parking_eligibility").Select(x => x.Value).ToArray(),
    });
}
