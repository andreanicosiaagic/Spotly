using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using Spotly.Api.Auth;
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
        var group = app.MapGroup("/api/desk").WithTags("Desk").RequireAuthorization("Employee");
        group.MapGet("/spots", async (IDeskRepository repo, TimeProvider clock, string? locationId, string? date) =>
        {
            var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            return !EndpointSupport.TryDate(date, today, out var bookingDate) ? Results.BadRequest()
                : Results.Ok(await repo.GetSpotsAsync(locationId ?? "HQ", bookingDate));
        });
        group.MapPost("/spots/{deskId}/lock", async (string deskId, HttpContext context, IDeskRepository repo) =>
        {
            var desk = await repo.GetSpotByIdAsync(deskId);
            if (desk is null) return Results.NotFound();
            if (desk.ReservedForDepartment is not null && !string.Equals(desk.ReservedForDepartment, CurrentUser.Department(context.User), StringComparison.OrdinalIgnoreCase))
                return Results.Json(new { error = "R-07: quota reparto non compatibile." }, statusCode: 403);
            return await repo.TryAcquireLockAsync(deskId, CurrentUser.Id(context.User), TimeSpan.FromMinutes(BookingRules.LockDurationMinutes))
                ? Results.Ok() : Results.Conflict(new { error = "R-03: risorsa non disponibile." });
        });
        group.MapPost("/bookings", async (CreateDeskBookingRequest request, HttpContext context, IDeskRepository repo,
            IHubContext<AvailabilityHub> hub, ICalendarIntegration calendar, IValidator<CreateDeskBookingRequest> validator,
            TimeProvider clock, IConfiguration configuration) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());
            var date = DateOnly.Parse(request.BookingDate); var now = clock.GetUtcNow().UtcDateTime;
            var error = BookingRules.ValidateBookingWindow(date, now);
            if (error is not null) return Results.UnprocessableEntity(new { error });
            var attempt = await repo.TryCreateBookingAsync(new DeskBooking
            {
                DeskId = request.DeskId, UserId = CurrentUser.Id(context.User), BookingDate = date,
                CheckInDeadlineUtc = EndpointSupport.CheckInDeadline(date, configuration),
            }, CurrentUser.Department(context.User));
            if (!attempt.Succeeded) return EndpointSupport.BookingFailureResult(attempt.Failure, "postazione");
            await calendar.CreateEventAsync(CurrentUser.Id(context.User), "Prenotazione postazione", date, request.DeskId);
            await AvailabilityHubExtensions.NotifyStatusChangeAsync(hub, "HQ", request.BookingDate,
                new AvailabilityUpdate(request.DeskId, "desk", "occupied"));
            return Results.Created($"/api/desk/bookings/{attempt.Booking!.BookingId}", attempt.Booking);
        });
        group.MapPost("/bookings/{bookingId}/check-in", async (string bookingId, HttpContext context, IDeskRepository repo,
            IAccessControlSystem access, TimeProvider clock) =>
        {
            var userId = CurrentUser.Id(context.User);
            if (!await access.ValidateCheckInAsync(userId, bookingId)) return Results.Forbid();
            return await repo.CheckInAsync(bookingId, userId, clock.GetUtcNow().UtcDateTime) ? Results.NoContent() : Results.Conflict();
        });
        group.MapDelete("/bookings/{bookingId}", async (string bookingId, HttpContext context, IDeskRepository repo,
            IHubContext<AvailabilityHub> hub, TimeProvider clock) =>
        {
            var outcome = await repo.CancelBookingAsync(bookingId, CurrentUser.Id(context.User), clock.GetUtcNow().UtcDateTime);
            if (!outcome.Found) return Results.NotFound();
            await AvailabilityHubExtensions.NotifyStatusChangeAsync(hub, "HQ", outcome.BookingDate!.Value.ToString("yyyy-MM-dd"),
                new AvailabilityUpdate(outcome.ResourceId!, "desk", "available"));
            return Results.Ok(new CancellationResponse(outcome.Status!.Value.ToString().ToLowerInvariant()));
        });
    }
}

public sealed class CreateDeskBookingValidator : AbstractValidator<CreateDeskBookingRequest>
{
    public CreateDeskBookingValidator()
    {
        RuleFor(x => x.DeskId).NotEmpty().MaximumLength(50);
        RuleFor(x => x.BookingDate).NotEmpty().Must(x => DateOnly.TryParse(x, out _)).WithMessage("BookingDate must use yyyy-MM-dd.");
    }
}
