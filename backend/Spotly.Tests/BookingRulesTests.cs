using Spotly.Domain.Rules;

namespace Spotly.Tests;

public class BookingRulesTests
{
    // ── R-01 ──────────────────────────────────────────────────────────────

    [Fact]
    public void R01_WhenNoExistingBooking_ReturnsNull()
    {
        var error = BookingRules.ValidateSingleBookingPerDay(false, "parcheggio");
        Assert.Null(error);
    }

    [Fact]
    public void R01_WhenExistingBooking_ReturnsError()
    {
        var error = BookingRules.ValidateSingleBookingPerDay(true, "parcheggio");
        Assert.NotNull(error);
        Assert.Contains("R-01", error);
    }

    // ── R-02 ──────────────────────────────────────────────────────────────

    [Fact]
    public void R02_TodayBooking_IsValid()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var error = BookingRules.ValidateBookingWindow(today);
        Assert.Null(error);
    }

    [Fact]
    public void R02_FutureDateWithinWindow_IsValid()
    {
        var future = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        var error = BookingRules.ValidateBookingWindow(future);
        Assert.Null(error);
    }

    [Fact]
    public void R02_PastDate_ReturnsError()
    {
        var past = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var error = BookingRules.ValidateBookingWindow(past);
        Assert.NotNull(error);
        Assert.Contains("R-02", error);
    }

    [Fact]
    public void R02_DateBeyondWindow_ReturnsError()
    {
        var beyond = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(BookingRules.MaxBookingWindowDays + 1);
        var error = BookingRules.ValidateBookingWindow(beyond);
        Assert.NotNull(error);
        Assert.Contains("R-02", error);
    }

    // ── R-06 ──────────────────────────────────────────────────────────────

    [Fact]
    public void R06_WhenRestaurantFull_LunchBoxIsAllowed()
    {
        var error = BookingRules.ValidateLunchBoxEligibility(restaurantHasAvailability: false, isOutsideHours: false);
        Assert.Null(error);
    }

    [Fact]
    public void R06_WhenRestaurantAvailable_LunchBoxIsNotAllowed()
    {
        var error = BookingRules.ValidateLunchBoxEligibility(restaurantHasAvailability: true, isOutsideHours: false);
        Assert.NotNull(error);
        Assert.Contains("R-06", error);
    }

    [Fact]
    public void R06_WhenOutsideHours_LunchBoxIsAllowed()
    {
        var error = BookingRules.ValidateLunchBoxEligibility(restaurantHasAvailability: true, isOutsideHours: true);
        Assert.Null(error);
    }

    // ── R-09 ──────────────────────────────────────────────────────────────

    [Fact]
    public void R09_FutureDate_IsFreeCancellation()
    {
        var future = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        Assert.True(BookingRules.IsFreeCancellation(future));
    }
}
