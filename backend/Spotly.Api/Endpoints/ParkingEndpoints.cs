using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using Spotly.Api.Auth;
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
        var group = app.MapGroup("/api/parking").WithTags("Parking").RequireAuthorization("Employee");

        group.MapGet("/spots", async (IParkingRepository repo, TimeProvider clock, string? locationId, string? date) =>
        {
            var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            return !EndpointSupport.TryDate(date, today, out var bookingDate)
                ? Results.ValidationProblem(new Dictionary<string, string[]> { ["date"] = ["Date must use yyyy-MM-dd."] })
                : Results.Ok(await repo.GetSpotsAsync(locationId ?? "HQ", bookingDate));
        });

        group.MapGet("/bookings/me", async (HttpContext context, IParkingRepository repo, TimeProvider clock, string? date) =>
        {
            var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            if (!EndpointSupport.TryDate(date, today, out var bookingDate)) return Results.BadRequest();
            var booking = await repo.GetActiveBookingAsync(CurrentUser.Id(context.User), bookingDate);
            return booking is null ? Results.NotFound() : Results.Ok(booking);
        });

        group.MapPost("/spots/{spotId}/lock", async (string spotId, HttpContext context, IParkingRepository repo, TimeProvider clock) =>
        {
            var spot = await repo.GetSpotByIdAsync(spotId);
            if (spot is null) return Results.NotFound();
            if (!CurrentUser.CanBook(context.User, spot.Type)) return Results.Json(new { error = "R-08: utente non idoneo al posto speciale." }, statusCode: 403);
            return await repo.TryAcquireLockAsync(spotId, CurrentUser.Id(context.User), TimeSpan.FromMinutes(BookingRules.LockDurationMinutes))
                ? Results.Ok(new { lockedUntilUtc = clock.GetUtcNow().AddMinutes(BookingRules.LockDurationMinutes) })
                : Results.Conflict(new { error = "R-03: risorsa non disponibile." });
        });

        group.MapPost("/bookings", async (CreateParkingBookingRequest request, HttpContext context, IParkingRepository repo,
            IHubContext<AvailabilityHub> hub, ICalendarIntegration calendar, IValidator<CreateParkingBookingRequest> validator,
            TimeProvider clock, IConfiguration configuration) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());
            var bookingDate = DateOnly.Parse(request.BookingDate);
            var now = clock.GetUtcNow().UtcDateTime;
            var windowError = BookingRules.ValidateBookingWindow(bookingDate, now);
            if (windowError is not null) return Results.UnprocessableEntity(new { error = windowError });
            var spot = await repo.GetSpotByIdAsync(request.SpotId);
            if (spot is null) return Results.NotFound();
            if (!CurrentUser.CanBook(context.User, spot.Type)) return Results.Json(new { error = "R-08: utente non idoneo al posto speciale." }, statusCode: 403);
            var attempt = await repo.TryCreateBookingAsync(new ParkingBooking
            {
                SpotId = request.SpotId,
                UserId = CurrentUser.Id(context.User),
                BookingDate = bookingDate,
                CheckInDeadlineUtc = EndpointSupport.CheckInDeadline(bookingDate, configuration),
            });
            if (!attempt.Succeeded) return EndpointSupport.BookingFailureResult(attempt.Failure, "parcheggio");
            await calendar.CreateEventAsync(CurrentUser.Id(context.User), "Prenotazione parcheggio", bookingDate, request.SpotId);
            await AvailabilityHubExtensions.NotifyStatusChangeAsync(hub, spot.LocationId, request.BookingDate,
                new AvailabilityUpdate(request.SpotId, "parking", "occupied"));
            return Results.Created($"/api/parking/bookings/{attempt.Booking!.BookingId}", attempt.Booking);
        });

        group.MapPost("/bookings/{bookingId}/check-in", async (string bookingId, HttpContext context, IParkingRepository repo,
            IAccessControlSystem access, TimeProvider clock) =>
        {
            var userId = CurrentUser.Id(context.User);
            if (!await access.ValidateCheckInAsync(userId, bookingId)) return Results.Forbid();
            return await repo.CheckInAsync(bookingId, userId, clock.GetUtcNow().UtcDateTime) ? Results.NoContent() : Results.Conflict();
        });

        group.MapDelete("/bookings/{bookingId}", async (string bookingId, HttpContext context, IParkingRepository repo,
            IHubContext<AvailabilityHub> hub, TimeProvider clock) =>
        {
            var outcome = await repo.CancelBookingAsync(bookingId, CurrentUser.Id(context.User), clock.GetUtcNow().UtcDateTime);
            if (!outcome.Found) return Results.NotFound();
            await AvailabilityHubExtensions.NotifyStatusChangeAsync(hub, "HQ", outcome.BookingDate!.Value.ToString("yyyy-MM-dd"),
                new AvailabilityUpdate(outcome.ResourceId!, "parking", "available"));
            return Results.Ok(new CancellationResponse(outcome.Status!.Value.ToString().ToLowerInvariant()));
        });
    }
}

public sealed class CreateParkingBookingValidator : AbstractValidator<CreateParkingBookingRequest>
{
    public CreateParkingBookingValidator()
    {
        RuleFor(x => x.SpotId).NotEmpty().MaximumLength(50);
        RuleFor(x => x.BookingDate).NotEmpty().Must(x => DateOnly.TryParse(x, out _)).WithMessage("BookingDate must use yyyy-MM-dd.");
    }
}
