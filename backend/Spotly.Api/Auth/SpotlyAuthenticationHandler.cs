using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Spotly.Api.Auth;

public sealed class SpotlyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IWebHostEnvironment environment)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthenticationScheme = "Spotly";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = environment.IsDevelopment() ? ReadDevelopmentClaims() : ReadEasyAuthClaims();
        if (claims.Count == 0) return Task.FromResult(AuthenticateResult.NoResult());
        var identity = new ClaimsIdentity(claims, AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), AuthenticationScheme)));
    }

    private List<Claim> ReadDevelopmentClaims()
    {
        if (TryReadDevelopmentToken(out var tokenClaims)) return tokenClaims;
        var userId = Request.Headers["X-Dev-User"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userId)) return [];
        var role = Request.Headers["X-Dev-Role"].FirstOrDefault() ?? "Dipendente";
        var claims = new List<Claim>
        {
            new("oid", userId),
            new(ClaimTypes.Name, $"Mock {role}"),
            new(ClaimTypes.Role, role),
        };
        AddOptionalClaim(claims, "department", Request.Headers["X-Dev-Department"].FirstOrDefault());
        foreach (var eligibility in Request.Headers["X-Dev-Parking-Eligibility"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            claims.Add(new Claim("parking_eligibility", eligibility));
        return claims;
    }

    private List<Claim> ReadEasyAuthClaims()
    {
        var easyAuthEnabled = string.Equals(Environment.GetEnvironmentVariable("WEBSITE_AUTH_ENABLED"), "true", StringComparison.OrdinalIgnoreCase);
        if (!easyAuthEnabled) return [];
        var encoded = Request.Headers["X-MS-CLIENT-PRINCIPAL"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(encoded)) return [];
        try
        {
            using var document = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(encoded)));
            var claims = new List<Claim>();
            foreach (var item in document.RootElement.GetProperty("claims").EnumerateArray())
            {
                var type = item.GetProperty("typ").GetString();
                var value = item.GetProperty("val").GetString();
                if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(value)) continue;
                if (type is "roles" or "role" or "http://schemas.microsoft.com/ws/2008/06/identity/claims/role") type = ClaimTypes.Role;
                if (type == "name") type = ClaimTypes.Name;
                if (type is "http://schemas.microsoft.com/identity/claims/objectidentifier" or "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                    type = type.Contains("objectidentifier", StringComparison.OrdinalIgnoreCase) ? "oid" : ClaimTypes.NameIdentifier;
                claims.Add(new Claim(type, value));
            }
            return claims;
        }
        catch (FormatException) { return []; }
        catch (JsonException) { return []; }
    }

    private bool TryReadDevelopmentToken(out List<Claim> claims)
    {
        claims = [];
        var bearer = Request.Headers.Authorization.FirstOrDefault();
        var accessToken = bearer?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true
            ? bearer["Bearer ".Length..]
            : Request.Query["access_token"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(accessToken) || !accessToken.StartsWith("dev.", StringComparison.Ordinal)) return false;

        try
        {
            var payload = accessToken["dev.".Length..];
            var padded = payload.Replace('-', '+').Replace('_', '/');
            while (padded.Length % 4 != 0) padded += "=";
            using var document = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(padded)));
            var root = document.RootElement;
            var userId = root.GetProperty("sub").GetString();
            var role = root.GetProperty("role").GetString();
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(role)) return false;
            claims.Add(new Claim("oid", userId));
            claims.Add(new Claim(ClaimTypes.Name, root.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? $"Mock {role}" : $"Mock {role}"));
            claims.Add(new Claim(ClaimTypes.Role, role));
            if (root.TryGetProperty("department", out var department) && !string.IsNullOrWhiteSpace(department.GetString()))
                claims.Add(new Claim("department", department.GetString()!));
            if (root.TryGetProperty("eligibility", out var eligibility) && eligibility.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in eligibility.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)))
                    claims.Add(new Claim("parking_eligibility", item!));
            }

            return true;
        }
        catch (Exception)
        {
            claims = [];
            return false;
        }
    }

    private static void AddOptionalClaim(List<Claim> claims, string type, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) claims.Add(new Claim(type, value));
    }
}
