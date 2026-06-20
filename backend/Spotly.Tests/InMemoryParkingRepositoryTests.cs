using Spotly.Domain.Entities;
using Spotly.Infrastructure.Repositories;

namespace Spotly.Tests;

public class InMemoryParkingRepositoryTests
{
    [Fact]
    public async Task GetSpotsAsync_ReturnsSeedData()
    {
        var repo = new InMemoryParkingRepository();
        var spots = await repo.GetSpotsAsync("HQ");
        Assert.NotEmpty(spots);
    }

    [Fact]
    public async Task CreateBooking_SetsSpotToOccupied()
    {
        var repo = new InMemoryParkingRepository();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var booking = new ParkingBooking
        {
            SpotId = "P01",
            UserId = "test-user",
            BookingDate = date,
            Status = BookingStatus.Active,
        };

        await repo.CreateBookingAsync(booking);

        var spots = (await repo.GetSpotsAsync("HQ")).ToList();
        var spot = spots.First(s => s.SpotId == "P01");
        Assert.Equal(ResourceStatus.Occupied, spot.Status);
    }

    [Fact]
    public async Task GetActiveBooking_ReturnsBookingAfterCreate()
    {
        var repo = new InMemoryParkingRepository();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        await repo.CreateBookingAsync(new ParkingBooking
        {
            SpotId = "P02",
            UserId = "u-test",
            BookingDate = date,
            Status = BookingStatus.Active,
        });

        var found = await repo.GetActiveBookingAsync("u-test", date);
        Assert.NotNull(found);
        Assert.Equal("P02", found.SpotId);
    }

    [Fact]
    public async Task CancelBooking_SetsSpotBackToAvailable()
    {
        var repo = new InMemoryParkingRepository();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var booking = await repo.CreateBookingAsync(new ParkingBooking
        {
            SpotId = "P04",
            UserId = "u-cancel",
            BookingDate = date,
            Status = BookingStatus.Active,
        });

        var result = await repo.CancelBookingAsync(booking.BookingId, "u-cancel");

        Assert.True(result);
        var spots = (await repo.GetSpotsAsync("HQ")).ToList();
        var spot = spots.First(s => s.SpotId == "P04");
        Assert.Equal(ResourceStatus.Available, spot.Status);
    }
}
