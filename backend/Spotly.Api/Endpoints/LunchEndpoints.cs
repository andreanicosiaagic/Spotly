using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using Spotly.Api.Auth;
using Spotly.Api.Dtos;
using Spotly.Api.Hubs;
using Spotly.Api.Services;
using Spotly.Domain.Entities;
using Spotly.Domain.Interfaces;
using Spotly.Domain.Rules;

namespace Spotly.Api.Endpoints;

public static class LunchEndpoints
{
    public static void MapLunchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/lunch").WithTags("Lunch").RequireAuthorization("Employee");
        group.MapGet("/restaurants", async (RestaurantLiveService live, TimeProvider clock, string? locationId, string? date) =>
        {
            var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            return !EndpointSupport.TryDate(date, today, out var bookingDate) ? Results.BadRequest()
                : Results.Ok(await live.GetAvailabilityAsync(locationId ?? "HQ", bookingDate));
        });
        group.MapGet("/partner-messages", async (ILunchRepository repo, int? take) => Results.Ok(
            (await repo.GetRecentPartnerMessagesAsync(take ?? 20)).Select(x => new RestaurantMessageEvent(
                x.RestaurantId, x.Kind, x.Outcome.ToString().ToLowerInvariant(), x.Sequence, x.ReceivedAtUtc))));
        group.MapGet("/slots", async (ILunchRepository repo, TimeProvider clock, string? locationId, string? date, string? restaurantId) =>
        {
            var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            return !EndpointSupport.TryDate(date, today, out var bookingDate) ? Results.BadRequest()
                : Results.Ok(await repo.GetSlotsByDateAsync(locationId ?? "HQ", bookingDate, restaurantId));
        });
        group.MapGet("/menu", async (ILunchRepository repo, TimeProvider clock, string? date, string? restaurantId) =>
        {
            var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            return !EndpointSupport.TryDate(date, today, out var menuDate) ? Results.BadRequest() : Results.Ok(await repo.GetMenuAsync(menuDate, restaurantId));
        });
        group.MapGet("/lunchboxes", async (ILunchRepository repo) => Results.Ok(await repo.GetLunchBoxesAsync()));
        group.MapPost("/bookings", async (CreateLunchBookingRequest request, HttpContext context, ILunchRepository repo,
            RestaurantLiveService live, IValidator<CreateLunchBookingRequest> validator, TimeProvider clock,
            CancellationToken cancellationToken) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());
            var date = DateOnly.Parse(request.BookingDate); var now = clock.GetUtcNow().UtcDateTime;
            var windowError = BookingRules.ValidateLunchBookingWindow(date, request.IsLunchBox, now);
            if (windowError is not null) return Results.UnprocessableEntity(new { error = windowError });
            var booking = new LunchBooking
            {
                RestaurantId = request.RestaurantId, SlotId = request.SlotId, UserId = CurrentUser.Id(context.User), BookingDate = date,
                IsLunchBox = request.IsLunchBox, LunchBoxId = request.LunchBoxId, Allergens = request.Allergens,
            };
            if (request.IsLunchBox)
            {
                var outsideHours = now.Hour is < 11 or >= 15;
                var lunchBoxAttempt = await repo.TryCreateBookingAsync(booking, outsideHours);
                if (!lunchBoxAttempt.Succeeded) return EndpointSupport.BookingFailureResult(lunchBoxAttempt.Failure, "pranzo");
                return Results.Created($"/api/lunch/bookings/{lunchBoxAttempt.Booking!.BookingId}", lunchBoxAttempt.Booking);
            }
            var result = await live.BookAsync(booking, cancellationToken);
            if (result.Attempt.Booking is null) return EndpointSupport.BookingFailureResult(result.Attempt.Failure, "pranzo");
            if (!result.Attempt.Succeeded) return Results.Conflict(new
            {
                error = $"Prenotazione rifiutata dal locale: {result.PartnerCode}",
                partnerCode = result.PartnerCode,
                availableSeats = result.AvailableSeats,
            });
            return Results.Created($"/api/lunch/bookings/{result.Attempt.Booking.BookingId}",
                new RestaurantBookingResponse(result.Attempt.Booking, result.PartnerCode, result.AvailableSeats, result.PartnerReference));
        });
        group.MapDelete("/bookings/{bookingId}", async (string bookingId, HttpContext context, ILunchRepository repo,
            RestaurantLiveService live, IHubContext<AvailabilityHub> hub, TimeProvider clock) =>
        {
            var outcome = await repo.CancelBookingAsync(bookingId, CurrentUser.Id(context.User), clock.GetUtcNow().UtcDateTime);
            if (!outcome.Found) return Results.NotFound();
            if (outcome.ResourceId is not null && outcome.BookingDate is not null)
            {
                var current = (await live.GetAvailabilityAsync("HQ", outcome.BookingDate.Value)).FirstOrDefault(x => x.RestaurantId == outcome.ResourceId);
                if (current is not null) await hub.Clients.Group($"availability:HQ:{outcome.BookingDate:yyyy-MM-dd}")
                    .SendAsync("RestaurantAvailabilityChanged", new RestaurantAvailabilityUpdate(current.RestaurantId, current.Name,
                        current.Capacity, current.AvailableSeats, current.Sequence, current.UpdatedAtUtc, "cancellation"));
            }
            return Results.Ok(new CancellationResponse(outcome.Status!.Value.ToString().ToLowerInvariant()));
        });

        group.MapPost("/demo/tick", async (RestaurantLiveService live, TimeProvider clock, string? date) =>
        {
            var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            if (!EndpointSupport.TryDate(date, today, out var bookingDate)) return Results.BadRequest();
            var results = await live.TickAsync("HQ", bookingDate);
            return Results.Ok(new { applied = results.Count(x => x.Outcome == PartnerMessageOutcome.Applied) });
        }).RequireAuthorization("Facility");
        group.MapPost("/demo/restaurants/{restaurantId}/availability", async (string restaurantId,
            SimulateRestaurantAvailabilityRequest request, RestaurantLiveService live, TimeProvider clock, string? date) =>
        {
            var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            if (!EndpointSupport.TryDate(date, today, out var bookingDate) || request.AvailableSeats < 0) return Results.BadRequest();
            var result = await live.SimulateAvailabilityAsync(restaurantId, bookingDate, request.AvailableSeats);
            return result.Outcome == PartnerMessageOutcome.Applied ? Results.Ok(result.Availability) : Results.Conflict(new { outcome = result.Outcome });
        }).RequireAuthorization("Facility");
        group.MapPost("/demo/restaurants/{restaurantId}/next-booking-outcome", (string restaurantId,
            SimulateBookingOutcomeRequest request, IRestaurantDemoGateway demo) =>
        {
            try { demo.SetNextBookingOutcome(restaurantId, request.Code); return Results.NoContent(); }
            catch (ArgumentOutOfRangeException) { return Results.BadRequest(new { error = "Code must be OK, FULL, CLOSED, INVALID_DATE, or TIMEOUT." }); }
        }).RequireAuthorization("Facility");
    }
}

public sealed class CreateLunchBookingValidator : AbstractValidator<CreateLunchBookingRequest>
{
    public CreateLunchBookingValidator()
    {
        RuleFor(x => x.BookingDate).NotEmpty().Must(x => DateOnly.TryParse(x, out _)).WithMessage("BookingDate must use yyyy-MM-dd.");
        RuleFor(x => x.Allergens).MaximumLength(500);
        When(x => x.IsLunchBox, () => RuleFor(x => x.LunchBoxId).NotEmpty());
        When(x => !x.IsLunchBox, () => RuleFor(x => x.RestaurantId).NotEmpty());
    }
}
