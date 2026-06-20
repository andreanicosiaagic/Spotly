using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using Spotly.Api.Dtos;
using Spotly.Api.Hubs;
using Spotly.Domain.Entities;
using Spotly.Domain.Interfaces;
using Spotly.Domain.Rules;

namespace Spotly.Api.Endpoints;

public static class DeskEndpoints
{
    public static void MapDeskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/desk").WithTags("Desk");

        group.MapGet("/spots", async (IDeskRepository repo, string? locationId, string? date) =>
        {
            var loc = locationId ?? "HQ";
            var spots = await repo.GetSpotsAsync(loc);
            return Results.Ok(spots);
        });

        group.MapPost("/bookings", async (
            CreateDeskBookingRequest req,
            IDeskRepository repo,
            IHubContext<AvailabilityHub> hub,
            IValidator<CreateDeskBookingRequest> validator) =>
        {
            var validation = await validator.ValidateAsync(req);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());

            var bookingDate = DateOnly.Parse(req.BookingDate);

            var windowError = BookingRules.ValidateBookingWindow(bookingDate);
            if (windowError is not null) return Results.Problem(windowError, statusCode: 422);

            var existing = await repo.GetActiveBookingAsync(req.UserId, bookingDate);
            var r01Error = BookingRules.ValidateSingleBookingPerDay(existing is not null, "postazione");
            if (r01Error is not null) return Results.Conflict(new { error = r01Error });

            var booking = new DeskBooking
            {
                DeskId = req.DeskId,
                UserId = req.UserId,
                BookingDate = bookingDate,
                Status = BookingStatus.Active,
            };

            var created = await repo.CreateBookingAsync(booking);

            await AvailabilityHubExtensions.NotifyStatusChangeAsync(hub, "HQ", req.BookingDate,
                new AvailabilityUpdate(req.DeskId, "desk", "occupied"));

            return Results.Created($"/api/desk/bookings/{created.BookingId}", created);
        });

        group.MapDelete("/bookings/{bookingId}", async (
            string bookingId,
            string userId,
            IDeskRepository repo,
            IHubContext<AvailabilityHub> hub) =>
        {
            var cancelled = await repo.CancelBookingAsync(bookingId, userId);
            if (!cancelled) return Results.NotFound();

            await AvailabilityHubExtensions.NotifyStatusChangeAsync(hub, "HQ",
                DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
                new AvailabilityUpdate(bookingId, "desk", "available"));

            return Results.NoContent();
        });
    }
}

public class CreateDeskBookingValidator : AbstractValidator<CreateDeskBookingRequest>
{
    public CreateDeskBookingValidator()
    {
        RuleFor(x => x.DeskId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.BookingDate).NotEmpty().Must(d => DateOnly.TryParse(d, out _)).WithMessage("BookingDate must be a valid date (yyyy-MM-dd).");
    }
}
