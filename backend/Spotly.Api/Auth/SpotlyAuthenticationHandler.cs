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
                claims.Add(new Claim(type, value));
            }
            return claims;
        }
        catch (FormatException) { return []; }
        catch (JsonException) { return []; }
    }

    private static void AddOptionalClaim(List<Claim> claims, string type, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) claims.Add(new Claim(type, value));
    }
}
