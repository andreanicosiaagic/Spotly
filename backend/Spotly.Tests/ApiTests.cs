using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Spotly.Domain.Entities;

namespace Spotly.Tests;

public class ApiTests
{
    [Fact]
    public async Task ProtectedEndpoint_WithoutPrincipal_ReturnsUnauthorized()
    {
        await using var app = new SpotlyApiFactory();
        var response = await app.CreateClient().GetAsync("/api/parking/spots", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BookingIdentity_ComesFromAuthenticatedPrincipal()
    {
        await using var app = new SpotlyApiFactory();
        var client = AuthenticatedClient(app, "claim-user");
        var date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var response = await client.PostAsJsonAsync("/api/parking/bookings", new { spotId = "P01", bookingDate = date, userId = "attacker" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var booking = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("claim-user", booking.GetProperty("userId").GetString());
    }

    [Fact]
    public async Task SpecialParking_RequiresEligibilityClaim()
    {
        await using var app = new SpotlyApiFactory();
        var client = AuthenticatedClient(app, "standard-user");
        var response = await client.PostAsJsonAsync("/api/parking/bookings", new
        {
            spotId = "P05", bookingDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DepartmentQuota_PrecedesIndividualDeskChoice()
    {
        await using var app = new SpotlyApiFactory();
        var client = AuthenticatedClient(app, "sales-user");
        client.DefaultRequestHeaders.Add("X-Dev-Department", "Sales");
        var response = await client.PostAsJsonAsync("/api/desk/bookings", new
        {
            deskId = "D07", bookingDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RestaurantBooking_ReturnsCodedConfirmationAndUpdatesSeats()
    {
        await using var app = new SpotlyApiFactory();
        var client = AuthenticatedClient(app, "lunch-user");
        var date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var before = await client.GetFromJsonAsync<JsonElement>($"/api/lunch/restaurants?date={date}", TestContext.Current.CancellationToken);
        var initialSeats = before.EnumerateArray().First(x => x.GetProperty("restaurantId").GetString() == "R01").GetProperty("availableSeats").GetInt32();
        var response = await client.PostAsJsonAsync("/api/lunch/bookings", new { restaurantId = "R01", isLunchBox = false, bookingDate = date },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("OK", result.GetProperty("partnerCode").GetString());
        Assert.Equal(initialSeats - 1, result.GetProperty("availableSeats").GetInt32());
    }

    [Fact]
    public async Task RestaurantBooking_PropagatesCodedFullErrorAndAuthoritativeSeats()
    {
        await using var app = new SpotlyApiFactory();
        var client = AuthenticatedClient(app, "facility-lunch-user", "Facility");
        var date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var configured = await client.PostAsJsonAsync("/api/lunch/demo/restaurants/R01/next-booking-outcome", new { code = "FULL" },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, configured.StatusCode);
        var response = await client.PostAsJsonAsync("/api/lunch/bookings", new { restaurantId = "R01", isLunchBox = false, bookingDate = date },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("FULL", error.GetProperty("partnerCode").GetString());
        Assert.Equal(0, error.GetProperty("availableSeats").GetInt32());
    }

    [Fact]
    public async Task TeamMatch_UsesTeamsLocationAndCalendarWithoutEventDetails()
    {
        await using var app = new SpotlyApiFactory();
        var client = AuthenticatedClient(app, "u2", "Manager");
        var response = await client.GetAsync("/api/collaboration/team-match", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("HQ", result.GetProperty("currentLocationId").GetString());
        Assert.Equal(2, result.GetProperty("matchingMembers").GetInt32());
        var json = result.GetRawText();
        Assert.DoesNotContain("subject", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("attendees", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Membro riservato", json);
    }

    [Fact]
    public async Task TeamMatch_RequiresManagerRole()
    {
        await using var app = new SpotlyApiFactory();
        var client = AuthenticatedClient(app, "u1");
        var response = await client.GetAsync("/api/collaboration/team-match", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static HttpClient AuthenticatedClient(SpotlyApiFactory app, string userId, string role = "Dipendente")
    {
        var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("X-Dev-User", userId);
        client.DefaultRequestHeaders.Add("X-Dev-Role", role);
        return client;
    }

    private sealed class SpotlyApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Name"] = $"spotly-tests-{Guid.NewGuid()}",
                ["Cors:AllowedOrigins:0"] = "https://localhost",
            }));
        }
    }
}
