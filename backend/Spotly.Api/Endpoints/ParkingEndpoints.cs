using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using Spotly.Api.Dtos;
using Spotly.Api.Hubs;
using Spotly.Domain.Entities;
using Spotly.Domain.Interfaces;
using Spotly.Domain.Rules;

namespace Spotly.Api.Endpoints;

public static class ParkingEndpoints
{
    public static void MapParkingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/parking").WithTags("Parking");

        group.MapGet("/spots", async (IParkingRepository repo, string? locationId, string? date) =>
        {
            var loc = locationId ?? "HQ";
            var d = date is not null ? DateOnly.Parse(date) : DateOnly.FromDateTime(DateTime.UtcNow);
            var spots = await repo.GetSpotsAsync(loc);
            return Results.Ok(spots);
        });

        group.MapPost("/bookings", async (
            CreateParkingBookingRequest req,
            IParkingRepository repo,
            IHubContext<AvailabilityHub> hub,
            IValidator<CreateParkingBookingRequest> validator) =>
        {
            var validation = await validator.ValidateAsync(req);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());

            var bookingDate = DateOnly.Parse(req.BookingDate);

            var windowError = BookingRules.ValidateBookingWindow(bookingDate);
            if (windowError is not null) return Results.Problem(windowError, statusCode: 422);

            var existing = await repo.GetActiveBookingAsync(req.UserId, bookingDate);
            var r01Error = BookingRules.ValidateSingleBookingPerDay(existing is not null, "parcheggio");
            if (r01Error is not null) return Results.Conflict(new { error = r01Error });

            var booking = new ParkingBooking
            {
                SpotId = req.SpotId,
                UserId = req.UserId,
                BookingDate = bookingDate,
                Status = BookingStatus.Active,
            };

            var created = await repo.CreateBookingAsync(booking);

            await AvailabilityHubExtensions.NotifyStatusChangeAsync(hub, "HQ", req.BookingDate,
                new AvailabilityUpdate(req.SpotId, "parking", "occupied"));

            return Results.Created($"/api/parking/bookings/{created.BookingId}", created);
        });

        group.MapDelete("/bookings/{bookingId}", async (
            string bookingId,
            string userId,
            IParkingRepository repo,
            IHubContext<AvailabilityHub> hub) =>
        {
            var cancelled = await repo.CancelBookingAsync(bookingId, userId);
            if (!cancelled) return Results.NotFound();

            await AvailabilityHubExtensions.NotifyStatusChangeAsync(hub, "HQ",
                DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
                new AvailabilityUpdate(bookingId, "parking", "available"));

            return Results.NoContent();
        });
    }
}

public class CreateParkingBookingValidator : AbstractValidator<CreateParkingBookingRequest>
{
    public CreateParkingBookingValidator()
    {
        RuleFor(x => x.SpotId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.BookingDate).NotEmpty().Must(d => DateOnly.TryParse(d, out _)).WithMessage("BookingDate must be a valid date (yyyy-MM-dd).");
    }
}
