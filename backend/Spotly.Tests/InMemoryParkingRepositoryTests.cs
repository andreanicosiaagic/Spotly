using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Spotly.Domain.Entities;
using Spotly.Infrastructure.Persistence;
using Spotly.Infrastructure.Repositories;

namespace Spotly.Tests;

public class InMemoryParkingRepositoryTests
{
    [Fact]
    public async Task ConcurrentBookingsForSameSpot_HaveExactlyOneWinner()
    {
        var repository = await CreateRepositoryAsync();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var attempts = await Task.WhenAll(Enumerable.Range(1, 12).Select(index => repository.TryCreateBookingAsync(new ParkingBooking
        {
            SpotId = "P01", UserId = $"user-{index}", BookingDate = date, CheckInDeadlineUtc = DateTime.UtcNow.AddHours(2),
        })));
        Assert.Single(attempts, x => x.Succeeded);
        Assert.Equal(11, attempts.Count(x => x.Failure == BookingFailure.ResourceUnavailable));
    }

    [Fact]
    public async Task SecondParkingBookingForUserAndDate_IsRejected()
    {
        var repository = await CreateRepositoryAsync();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var first = await repository.TryCreateBookingAsync(NewBooking("P01", "same-user", date));
        var second = await repository.TryCreateBookingAsync(NewBooking("P02", "same-user", date));
        Assert.True(first.Succeeded);
        Assert.Equal(BookingFailure.AlreadyBooked, second.Failure);
    }

    [Fact]
    public async Task ExpiredUncheckedBooking_IsReleasedAsNoShow()
    {
        var repository = await CreateRepositoryAsync();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var created = await repository.TryCreateBookingAsync(new ParkingBooking
        {
            SpotId = "P01", UserId = "late-user", BookingDate = date, CheckInDeadlineUtc = DateTime.UtcNow.AddMinutes(-1),
        });
        var released = await repository.ReleaseNoShowsAsync(DateTime.UtcNow);
        Assert.True(created.Succeeded);
        Assert.Contains(released, x => x.ResourceId == "P01");
        Assert.Null(await repository.GetActiveBookingAsync("late-user", date));
    }

    private static ParkingBooking NewBooking(string spotId, string userId, DateOnly date) => new()
    {
        SpotId = spotId, UserId = userId, BookingDate = date, CheckInDeadlineUtc = DateTime.UtcNow.AddHours(2),
    };

    private static async Task<InMemoryParkingRepository> CreateRepositoryAsync()
    {
        var options = new DbContextOptionsBuilder<SpotlyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), new InMemoryDatabaseRoot()).Options;
        var factory = new TestDbContextFactory(options);
        await using var db = factory.CreateDbContext();
        await SpotlyDbSeeder.SeedAsync(db, DateOnly.FromDateTime(DateTime.UtcNow));
        return new InMemoryParkingRepository(factory);
    }

    private sealed class TestDbContextFactory(DbContextOptions<SpotlyDbContext> options) : IDbContextFactory<SpotlyDbContext>
    {
        public SpotlyDbContext CreateDbContext() => new(options);
    }
}
