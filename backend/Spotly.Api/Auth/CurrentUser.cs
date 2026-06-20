using System.Security.Claims;
using Spotly.Domain.Entities;

namespace Spotly.Api.Auth;

public static class CurrentUser
{
    public static string Id(ClaimsPrincipal user) => user.FindFirst("oid")?.Value
        ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new InvalidOperationException("Authenticated principal has no stable identifier.");

    public static string? Department(ClaimsPrincipal user) => user.FindFirst("department")?.Value;

    public static bool CanBook(ClaimsPrincipal user, ParkingSpotType type) => type switch
    {
        ParkingSpotType.Standard => true,
        ParkingSpotType.Ev => HasEligibility(user, "ev"),
        ParkingSpotType.Disabled => HasEligibility(user, "disabled"),
        ParkingSpotType.Guest => HasEligibility(user, "guest") || user.IsInRole("Facility") || user.IsInRole("Admin"),
        _ => false,
    };

    private static bool HasEligibility(ClaimsPrincipal user, string value) => user.FindAll("parking_eligibility")
        .Any(x => string.Equals(x.Value, value, StringComparison.OrdinalIgnoreCase));
}
