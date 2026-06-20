using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Spotly.Domain.Entities;
using Spotly.Infrastructure.Integrations;
using Spotly.Infrastructure.Persistence;
using Spotly.Infrastructure.Repositories;

namespace Spotly.Tests;

public class RestaurantMessagingTests
{
    [Fact]
    public void AvailabilityProtocol_RoundTripsStandardMessage()
    {
        var protocol = new RestaurantPartnerProtocol();
        var source = new PartnerAvailabilityMessage("msg-42", "R01", new DateOnly(2026, 6, 20), 17, 42,
            new DateTime(2026, 6, 20, 11, 50, 0, DateTimeKind.Utc));
        var payload = protocol.EncodeAvailability(source);
        Assert.True(protocol.TryDecodeAvailability(payload, out var decoded));
        Assert.Equal(source, decoded);
    }

    [Fact]
    public async Task AvailabilityInbox_RejectsDuplicateAndStaleMessages()
    {
        var repository = await CreateRepositoryAsync();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var first = new PartnerAvailabilityMessage("same-id", "R01", date, 12, 10, DateTime.UtcNow);
        var applied = await repository.ApplyAvailabilityAsync(first, "payload", DateTime.UtcNow);
        var duplicate = await repository.ApplyAvailabilityAsync(first, "payload", DateTime.UtcNow);
        var stale = await repository.ApplyAvailabilityAsync(first with { MessageId = "other-id", Sequence = 9 }, "payload", DateTime.UtcNow);
        Assert.Equal(PartnerMessageOutcome.Applied, applied.Outcome);
        Assert.Equal(PartnerMessageOutcome.Duplicate, duplicate.Outcome);
        Assert.Equal(PartnerMessageOutcome.Stale, stale.Outcome);
    }

    [Fact]
    public async Task ConfirmedBooking_UsesPartnerRemainingSeatsAsAuthoritativeSnapshot()
    {
        var repository = await CreateRepositoryAsync();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = await repository.BeginRestaurantBookingAsync(new LunchBooking
        {
            RestaurantId = "R01",
            SlotId = $"S01-{date:yyyyMMdd}",
            UserId = "restaurant-user",
            BookingDate = date,
            MenuItemIds = [$"M01-{date:yyyyMMdd}"],
        });
        Assert.True(start.Attempt.Succeeded);
        var pending = start.Attempt.Booking!;
        var completed = await repository.CompleteRestaurantBookingAsync(pending.PartnerCorrelationId!,
            new(pending.PartnerCorrelationId!, "R01", date, "OK", 11, "TG-REF"), DateTime.UtcNow);
        var availability = Assert.Single(await repository.GetRestaurantAvailabilityAsync("HQ", date), x => x.RestaurantId == "R01");
        Assert.True(completed.Succeeded);
        Assert.Equal(PartnerBookingStatus.Confirmed, completed.Booking!.PartnerStatus);
        Assert.Equal(11, availability.AvailableSeats);
    }

    [Fact]
    public void SqlServerModel_ProducesRestaurantMessagingSchema()
    {
        var options = new DbContextOptionsBuilder<SpotlyDbContext>()
            .UseSqlServer("Server=(local);Database=SpotlyModelValidation;Integrated Security=True;TrustServerCertificate=True").Options;
        using var db = new SpotlyDbContext(options);
        var script = db.Database.GenerateCreateScript();
        Assert.Contains("[Restaurants]", script);
        Assert.Contains("[WhatsAppNumber]", script);
        Assert.Contains("[RestaurantAvailabilities]", script);
        Assert.Contains("[RestaurantPartnerMessages]", script);
        Assert.Contains("[PartnerCorrelationId]", script);
    }

    private static async Task<InMemoryLunchRepository> CreateRepositoryAsync()
    {
        var options = new DbContextOptionsBuilder<SpotlyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), new InMemoryDatabaseRoot()).Options;
        var factory = new TestDbContextFactory(options);
        await using var db = factory.CreateDbContext();
        await SpotlyDbSeeder.SeedAsync(db, DateOnly.FromDateTime(DateTime.UtcNow));
        return new(factory, TimeProvider.System);
    }

    private sealed class TestDbContextFactory(DbContextOptions<SpotlyDbContext> options) : IDbContextFactory<SpotlyDbContext>
    {
        public SpotlyDbContext CreateDbContext() => new(options);
    }
}
