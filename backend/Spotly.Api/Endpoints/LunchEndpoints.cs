using FluentValidation;
using Spotly.Api.Dtos;
using Spotly.Domain.Entities;
using Spotly.Domain.Interfaces;
using Spotly.Domain.Rules;

namespace Spotly.Api.Endpoints;

public static class LunchEndpoints
{
    public static void MapLunchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/lunch").WithTags("Lunch");

        group.MapGet("/restaurants", async (ILunchRepository repo, string? locationId) =>
            Results.Ok(await repo.GetRestaurantsAsync(locationId ?? "HQ")));

        group.MapGet("/slots", async (ILunchRepository repo, string? locationId, string? date, string? restaurantId) =>
        {
            var d = date is not null ? DateOnly.Parse(date) : DateOnly.FromDateTime(DateTime.UtcNow);
            return Results.Ok(await repo.GetSlotsByDateAsync(locationId ?? "HQ", d, restaurantId));
        });

        group.MapGet("/menu", async (ILunchRepository repo, string? date, string? restaurantId) =>
        {
            var d = date is not null ? DateOnly.Parse(date) : DateOnly.FromDateTime(DateTime.UtcNow);
            return Results.Ok(await repo.GetMenuAsync(d, restaurantId));
        });

        group.MapGet("/lunchboxes", async (ILunchRepository repo) =>
            Results.Ok(await repo.GetLunchBoxesAsync()));

        group.MapPost("/bookings", async (
            CreateLunchBookingRequest req,
            ILunchRepository repo,
            IValidator<CreateLunchBookingRequest> validator) =>
        {
            var validation = await validator.ValidateAsync(req);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());

            var bookingDate = DateOnly.Parse(req.BookingDate);

            var windowError = BookingRules.ValidateLunchBookingWindow(bookingDate);
            if (windowError is not null) return Results.Problem(windowError, statusCode: 422);

            var existing = await repo.GetActiveBookingAsync(req.UserId, bookingDate);
            var r01Error = BookingRules.ValidateSingleBookingPerDay(existing is not null, "pranzo");
            if (r01Error is not null) return Results.Conflict(new { error = r01Error });

            // R-06: lunch box only when slot is full
            if (!req.IsLunchBox && req.SlotId is not null)
            {
                var ok = await repo.TryDecrementSlotAsync(req.SlotId);
                if (!ok) return Results.Conflict(new { error = "R-06: slot esaurito — usa il lunch box" });
            }

            var booking = new LunchBooking
            {
                RestaurantId = req.RestaurantId,
                SlotId = req.SlotId,
                UserId = req.UserId,
                BookingDate = bookingDate,
                IsLunchBox = req.IsLunchBox,
                LunchBoxId = req.LunchBoxId,
                Allergens = req.Allergens,
                Status = BookingStatus.Active,
                DeliveryStatus = DeliveryStatus.Pending,
            };

            var created = await repo.CreateBookingAsync(booking);
            return Results.Created($"/api/lunch/bookings/{created.BookingId}", created);
        });

        group.MapDelete("/bookings/{bookingId}", async (string bookingId, string userId, ILunchRepository repo) =>
        {
            var cancelled = await repo.CancelBookingAsync(bookingId, userId);
            return cancelled ? Results.NoContent() : Results.NotFound();
        });
    }
}

public class CreateLunchBookingValidator : AbstractValidator<CreateLunchBookingRequest>
{
    public CreateLunchBookingValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.BookingDate).NotEmpty().Must(d => DateOnly.TryParse(d, out _)).WithMessage("BookingDate must be a valid date.");
        When(x => !x.IsLunchBox, () =>
        {
            RuleFor(x => x.RestaurantId).NotEmpty().WithMessage("RestaurantId is required for restaurant booking.");
            RuleFor(x => x.SlotId).NotEmpty().WithMessage("SlotId is required for restaurant booking.");
        });
        When(x => x.IsLunchBox, () =>
            RuleFor(x => x.LunchBoxId).NotEmpty().WithMessage("LunchBoxId is required for lunch box order."));
    }
}
