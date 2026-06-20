using System.Globalization;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using Spotly.Api.Auth;
using Spotly.Api.Dtos;
using Spotly.Api.Endpoints;
using Spotly.Api.Hubs;
using Spotly.Domain.Entities;
using Spotly.Domain.Interfaces;
using Spotly.Domain.Rules;

namespace Spotly.Api.Services;

public sealed class DeskBookingService(
    IDeskRepository repo,
    IHubContext<AvailabilityHub> hub,
    ICalendarIntegration calendar,
    IValidator<CreateDeskBookingRequest> validator,
    OfficeTime officeTime)
{
    public async Task<IResult> GetSpotsAsync(string? locationId, string? date)
    {
        if (!EndpointSupport.TryDate(date, officeTime.Today, out var bookingDate)) return EndpointSupport.InvalidDate();
        return Results.Ok(await repo.GetSpotsAsync(locationId ?? "HQ", bookingDate));
    }

    public async Task<IResult> GetMyBookingAsync(ClaimsPrincipal user, string? date)
    {
        if (!EndpointSupport.TryDate(date, officeTime.Today, out var bookingDate)) return EndpointSupport.InvalidDate();
        var booking = await repo.GetActiveBookingAsync(CurrentUser.Id(user), bookingDate);
        return booking is null
            ? EndpointSupport.Problem(StatusCodes.Status404NotFound, "BOOKING_NOT_FOUND", "Nessuna prenotazione postazione attiva per la data selezionata.")
            : Results.Ok(booking);
    }

    public async Task<IResult> LockAsync(string deskId, ClaimsPrincipal user, string? date)
    {
        if (!EndpointSupport.TryDate(date, officeTime.Today, out var bookingDate)) return EndpointSupport.InvalidDate();
        var desk = await repo.GetSpotByIdAsync(deskId);
        if (desk is null) return EndpointSupport.Problem(StatusCodes.Status404NotFound, "RESOURCE_NOT_FOUND", "Risorsa non trovata.");
        if (desk.ReservedForDepartment is not null && !string.Equals(desk.ReservedForDepartment, CurrentUser.Department(user), StringComparison.OrdinalIgnoreCase))
            return EndpointSupport.Problem(StatusCodes.Status403Forbidden, "R07_QUOTA_MISMATCH", "R-07: quota reparto non compatibile.");

        var acquired = await repo.TryAcquireLockAsync(deskId, CurrentUser.Id(user), bookingDate, TimeSpan.FromMinutes(BookingRules.LockDurationMinutes));
        return acquired
            ? Results.Ok(new LockAcquisitionResponse(officeTime.UtcNow.UtcDateTime.AddMinutes(BookingRules.LockDurationMinutes), bookingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)))
            : EndpointSupport.Problem(StatusCodes.Status409Conflict, "R03_RESOURCE_UNAVAILABLE", "R-03: risorsa non disponibile.");
    }

    public async Task<IResult> CreateBookingAsync(CreateDeskBookingRequest request, ClaimsPrincipal user)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());

        var bookingDate = DateOnly.ParseExact(request.BookingDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var windowError = BookingRules.ValidateBookingWindow(bookingDate, officeTime.LocalNow)
            ?? BookingRules.ValidateSameDayBookingBeforeCheckInClose(bookingDate, officeTime.LocalNow, officeTime.CheckInClosesLocal);
        if (windowError is not null) return EndpointSupport.Problem(StatusCodes.Status422UnprocessableEntity, "BOOKING_WINDOW_INVALID", windowError);

        var checkInWindow = officeTime.GetCheckInWindow(bookingDate);
        var attempt = await repo.TryCreateBookingAsync(new DeskBooking
        {
            DeskId = request.DeskId,
            UserId = CurrentUser.Id(user),
            BookingDate = bookingDate,
            CreatedAtUtc = officeTime.UtcNow.UtcDateTime,
            CheckInOpensAtUtc = checkInWindow.OpensAtUtc,
            CheckInDeadlineUtc = checkInWindow.ClosesAtUtc,
        }, CurrentUser.Department(user));

        if (!attempt.Succeeded) return EndpointSupport.BookingFailureResult(attempt.Failure, "postazione");

        await calendar.CreateEventAsync(CurrentUser.Id(user), "Prenotazione postazione", bookingDate, request.DeskId);
        await AvailabilityHubExtensions.NotifyStatusChangeAsync(hub, "HQ", request.BookingDate,
            new AvailabilityUpdate(request.DeskId, "desk", "occupied"));
        return Results.Created($"/api/desk/bookings/{attempt.Booking!.BookingId}", attempt.Booking);
    }

    public async Task<IResult> CheckInAsync(string bookingId, ClaimsPrincipal user, IAccessControlSystem access)
    {
        var userId = CurrentUser.Id(user);
        if (!await access.ValidateCheckInAsync(userId, bookingId)) return Results.Forbid();
        return await repo.CheckInAsync(bookingId, userId, officeTime.UtcNow.UtcDateTime)
            ? Results.NoContent()
            : EndpointSupport.Problem(StatusCodes.Status409Conflict, "CHECKIN_WINDOW_INVALID", "R-04: check-in non consentito per data o finestra corrente.");
    }

    public async Task<IResult> CancelAsync(string bookingId, ClaimsPrincipal user)
    {
        var outcome = await repo.CancelBookingAsync(bookingId, CurrentUser.Id(user), officeTime.LocalNow);
        if (!outcome.Found) return EndpointSupport.Problem(StatusCodes.Status404NotFound, "BOOKING_NOT_FOUND", "Prenotazione postazione non trovata.");
        await AvailabilityHubExtensions.NotifyStatusChangeAsync(hub, "HQ", outcome.BookingDate!.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            new AvailabilityUpdate(outcome.ResourceId!, "desk", "available"));
        return Results.Ok(new CancellationResponse(outcome.Status!.Value.ToString().ToLowerInvariant()));
    }
}
