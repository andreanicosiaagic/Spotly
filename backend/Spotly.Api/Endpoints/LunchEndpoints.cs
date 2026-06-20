using System.Globalization;
using FluentValidation;
using Spotly.Api.Dtos;
using Spotly.Api.Services;

namespace Spotly.Api.Endpoints;

public static class LunchEndpoints
{
    public static void MapLunchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/lunch").WithTags("Lunch").RequireAuthorization("Employee");

        group.MapGet("/restaurants", (LunchBookingService service, string? locationId, string? date) => service.GetRestaurantsAsync(locationId, date));
        group.MapGet("/partner-messages", (LunchBookingService service, int? take, string? date) => service.GetPartnerMessagesAsync(take, date));
        group.MapGet("/slots", (LunchBookingService service, string? locationId, string? date, string? restaurantId) => service.GetSlotsAsync(locationId, date, restaurantId));
        group.MapGet("/menu", (LunchBookingService service, string? date, string? restaurantId) => service.GetMenuAsync(date, restaurantId));
        group.MapGet("/lunchboxes", (LunchBookingService service) => service.GetLunchBoxesAsync());
        group.MapGet("/bookings/me", (HttpContext context, LunchBookingService service, string? date) => service.GetMyBookingAsync(context.User, date));
        group.MapGet("/lunchbox-eligibility", (LunchBookingService service, string? locationId, string? date) => service.GetLunchBoxEligibilityAsync(locationId, date));
        group.MapPost("/bookings", (CreateLunchBookingRequest request, HttpContext context, LunchBookingService service, CancellationToken cancellationToken) => service.CreateBookingAsync(request, context.User, cancellationToken));
        group.MapDelete("/bookings/{bookingId}", (string bookingId, HttpContext context, LunchBookingService service, CancellationToken cancellationToken) => service.CancelAsync(bookingId, context.User, cancellationToken));
        group.MapPost("/demo/tick", (LunchBookingService service, string? date) => service.TickAsync(date)).RequireAuthorization("Facility");
        group.MapPost("/demo/restaurants/{restaurantId}/availability", (string restaurantId, SimulateRestaurantAvailabilityRequest request, LunchBookingService service, string? date) => service.SimulateAvailabilityAsync(restaurantId, request, date)).RequireAuthorization("Facility");
        group.MapPost("/demo/restaurants/{restaurantId}/next-booking-outcome", (string restaurantId, SimulateBookingOutcomeRequest request, LunchBookingService service) => service.SetNextBookingOutcome(restaurantId, request)).RequireAuthorization("Facility");
    }
}

public sealed class CreateLunchBookingValidator : AbstractValidator<CreateLunchBookingRequest>
{
    public CreateLunchBookingValidator()
    {
        RuleFor(x => x.BookingDate)
            .NotEmpty()
            .Must(x => DateOnly.TryParseExact(x, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            .WithMessage("BookingDate must use yyyy-MM-dd.");
        RuleFor(x => x.Allergens).MaximumLength(500);
        When(x => x.IsLunchBox, () => RuleFor(x => x.LunchBoxId).NotEmpty());
        When(x => !x.IsLunchBox, () =>
        {
            RuleFor(x => x.RestaurantId).NotEmpty();
            RuleFor(x => x.SlotId).NotEmpty();
            RuleFor(x => x.MenuItemIds).NotNull().Must(items => items is { Count: > 0 }).WithMessage("Select at least one menu item.");
        });
    }
}
