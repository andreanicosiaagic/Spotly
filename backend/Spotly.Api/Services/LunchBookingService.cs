using System.Globalization;
using System.Security.Claims;
using FluentValidation;
using Spotly.Api.Auth;
using Spotly.Api.Dtos;
using Spotly.Api.Endpoints;
using Spotly.Domain.Entities;
using Spotly.Domain.Interfaces;
using Spotly.Domain.Rules;

namespace Spotly.Api.Services;

public sealed class LunchBookingService(
    ILunchRepository repo,
    RestaurantLiveService live,
    IRestaurantDemoGateway demoGateway,
    IValidator<CreateLunchBookingRequest> validator,
    OfficeTime officeTime)
{
    public async Task<IResult> GetRestaurantsAsync(string? locationId, string? date)
    {
        if (!EndpointSupport.TryDate(date, officeTime.Today, out var bookingDate)) return EndpointSupport.InvalidDate();
        return Results.Ok(await live.GetAvailabilityAsync(locationId ?? "HQ", bookingDate));
    }

    public async Task<IResult> GetPartnerMessagesAsync(int? take, string? date)
    {
        if (!EndpointSupport.TryDate(date, officeTime.Today, out var bookingDate)) return EndpointSupport.InvalidDate();
        var items = await repo.GetRecentPartnerMessagesAsync(bookingDate, take ?? 20);
        return Results.Ok(items.Select(x => new RestaurantMessageEvent(
            x.LocationId,
            x.BookingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            x.RestaurantId,
            x.Kind,
            x.Outcome.ToString().ToLowerInvariant(),
            x.Sequence,
            x.ReceivedAtUtc)));
    }

    public async Task<IResult> GetSlotsAsync(string? locationId, string? date, string? restaurantId)
    {
        if (!EndpointSupport.TryDate(date, officeTime.Today, out var bookingDate)) return EndpointSupport.InvalidDate();
        return Results.Ok(await repo.GetSlotsByDateAsync(locationId ?? "HQ", bookingDate, restaurantId));
    }

    public async Task<IResult> GetMenuAsync(string? date, string? restaurantId)
    {
        if (!EndpointSupport.TryDate(date, officeTime.Today, out var menuDate)) return EndpointSupport.InvalidDate();
        return Results.Ok(await repo.GetMenuAsync(menuDate, restaurantId));
    }

    public async Task<IResult> GetLunchBoxesAsync() => Results.Ok(await repo.GetLunchBoxesAsync());

    public async Task<IResult> GetMyBookingAsync(ClaimsPrincipal user, string? date)
    {
        if (!EndpointSupport.TryDate(date, officeTime.Today, out var bookingDate)) return EndpointSupport.InvalidDate();
        var booking = await repo.GetActiveBookingAsync(CurrentUser.Id(user), bookingDate);
        return booking is null
            ? EndpointSupport.Problem(StatusCodes.Status404NotFound, "BOOKING_NOT_FOUND", "Nessuna prenotazione pranzo attiva per la data selezionata.")
            : Results.Ok(booking);
    }

    public async Task<IResult> GetLunchBoxEligibilityAsync(string? locationId, string? date)
    {
        if (!EndpointSupport.TryDate(date, officeTime.Today, out var bookingDate)) return EndpointSupport.InvalidDate();
        var restaurants = await live.GetAvailabilityAsync(locationId ?? "HQ", bookingDate);
        var restaurantsFull = restaurants.Count > 0 && restaurants.All(x => x.AvailableSeats <= 0);
        var outsideOperatingHours = officeTime.IsOutsideLunchServiceWindow();
        var reason = BookingRules.ValidateLunchBookingWindow(bookingDate, true, officeTime.LocalNow)
            ?? BookingRules.ValidateLunchBoxEligibility(!restaurantsFull, outsideOperatingHours)
            ?? "Lunch box disponibile per la data selezionata.";

        return Results.Ok(new LunchBoxEligibilityResponse(
            Eligible: reason == "Lunch box disponibile per la data selezionata.",
            Reason: reason,
            CutoffLocal: "23:59",
            RestaurantsFull: restaurantsFull,
            OutsideOperatingHours: outsideOperatingHours,
            DemoDate: officeTime.Today.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
    }

    public async Task<IResult> CreateBookingAsync(CreateLunchBookingRequest request, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());

        var bookingDate = DateOnly.ParseExact(request.BookingDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var windowError = BookingRules.ValidateLunchBookingWindow(bookingDate, request.IsLunchBox, officeTime.LocalNow);
        if (windowError is not null) return EndpointSupport.Problem(StatusCodes.Status422UnprocessableEntity, "BOOKING_WINDOW_INVALID", windowError);

        var booking = new LunchBooking
        {
            RestaurantId = request.RestaurantId,
            SlotId = request.SlotId,
            UserId = CurrentUser.Id(user),
            BookingDate = bookingDate,
            IsLunchBox = request.IsLunchBox,
            LunchBoxId = request.LunchBoxId,
            Allergens = request.Allergens,
            MenuItemIds = request.MenuItemIds?.Distinct().ToList() ?? [],
            CreatedAtUtc = officeTime.UtcNow.UtcDateTime,
        };

        if (request.IsLunchBox)
        {
            var lunchBoxAttempt = await repo.TryCreateBookingAsync(booking, officeTime.IsOutsideLunchServiceWindow());
            return lunchBoxAttempt.Succeeded
                ? Results.Created($"/api/lunch/bookings/{lunchBoxAttempt.Booking!.BookingId}", lunchBoxAttempt.Booking)
                : EndpointSupport.BookingFailureResult(lunchBoxAttempt.Failure, "pranzo");
        }

        var result = await live.BookAsync(booking, cancellationToken);
        if (result.Attempt.Booking is null) return EndpointSupport.BookingFailureResult(result.Attempt.Failure, "pranzo");
        if (!result.Attempt.Succeeded)
        {
            return Results.Problem(
                title: result.PartnerCode,
                detail: $"Prenotazione rifiutata dal locale: {result.PartnerCode}",
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "PARTNER_REJECTED",
                    ["partnerCode"] = result.PartnerCode,
                    ["availableSeats"] = result.AvailableSeats,
                });
        }

        return Results.Created($"/api/lunch/bookings/{result.Attempt.Booking.BookingId}",
            new RestaurantBookingResponse(result.Attempt.Booking, result.PartnerCode, result.AvailableSeats, result.PartnerReference));
    }

    public async Task<IResult> CancelAsync(string bookingId, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var userId = CurrentUser.Id(user);
        var booking = await repo.GetUserBookingAsync(bookingId, userId);
        if (booking is null || booking.Status != BookingStatus.Active)
            return EndpointSupport.Problem(StatusCodes.Status404NotFound, "BOOKING_NOT_FOUND", "Prenotazione pranzo non trovata.");

        if (!booking.IsLunchBox && !await live.CancelPartnerBookingAsync(booking, cancellationToken))
            return EndpointSupport.Problem(StatusCodes.Status502BadGateway, "PARTNER_CANCELLATION_FAILED", "Il locale non ha confermato l'annullamento.");

        var outcome = await repo.CancelBookingAsync(bookingId, userId, officeTime.LocalNow);
        if (!outcome.Found) return EndpointSupport.Problem(StatusCodes.Status404NotFound, "BOOKING_NOT_FOUND", "Prenotazione pranzo non trovata.");
        if (!booking.IsLunchBox && booking.RestaurantId is not null && outcome.BookingDate is not null)
            await live.BroadcastAvailabilityAsync("HQ", outcome.BookingDate.Value, booking.RestaurantId, "cancellation", cancellationToken);
        return Results.Ok(new CancellationResponse(outcome.Status!.Value.ToString().ToLowerInvariant()));
    }

    public async Task<IResult> TickAsync(string? date)
    {
        if (!EndpointSupport.TryDate(date, officeTime.Today, out var bookingDate)) return EndpointSupport.InvalidDate();
        var results = await live.TickAsync("HQ", bookingDate);
        return Results.Ok(new { applied = results.Count(x => x.Outcome == PartnerMessageOutcome.Applied) });
    }

    public async Task<IResult> SimulateAvailabilityAsync(string restaurantId, SimulateRestaurantAvailabilityRequest request, string? date)
    {
        if (!EndpointSupport.TryDate(date, officeTime.Today, out var bookingDate)) return EndpointSupport.InvalidDate();
        if (request.AvailableSeats < 0)
            return EndpointSupport.Problem(StatusCodes.Status400BadRequest, "INVALID_SEAT_COUNT", "AvailableSeats must be greater than or equal to zero.");
        var result = await live.SimulateAvailabilityAsync(restaurantId, bookingDate, request.AvailableSeats);
        return result.Outcome == PartnerMessageOutcome.Applied
            ? Results.Ok(result.Availability)
            : EndpointSupport.Problem(StatusCodes.Status409Conflict, "PARTNER_MESSAGE_REJECTED", $"Outcome: {result.Outcome}");
    }

    public IResult SetNextBookingOutcome(string restaurantId, SimulateBookingOutcomeRequest request)
    {
        try
        {
            demoGateway.SetNextBookingOutcome(restaurantId, request.Code);
            return Results.NoContent();
        }
        catch (ArgumentOutOfRangeException)
        {
            return EndpointSupport.Problem(StatusCodes.Status400BadRequest, "INVALID_PARTNER_CODE", "Code must be OK, FULL, CLOSED, INVALID_DATE, or TIMEOUT.");
        }
    }
}
