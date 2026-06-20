using Microsoft.AspNetCore.SignalR;
using Spotly.Api.Dtos;
using Spotly.Api.Hubs;
using Spotly.Domain.Entities;
using Spotly.Domain.Interfaces;

namespace Spotly.Api.Services;

public sealed record RestaurantBookingServiceResult(
    BookingAttempt<LunchBooking> Attempt,
    string PartnerCode,
    int AvailableSeats,
    string? PartnerReference);

public sealed class RestaurantLiveService(
    ILunchRepository repository,
    IRestaurantMessagingGateway gateway,
    IRestaurantPartnerProtocol protocol,
    IHubContext<AvailabilityHub> hub,
    OfficeTime officeTime,
    ILogger<RestaurantLiveService> logger)
{
    public Task<IReadOnlyList<RestaurantAvailabilityView>> GetAvailabilityAsync(string locationId, DateOnly date) =>
        repository.GetRestaurantAvailabilityAsync(locationId, date);

    public async Task<RestaurantMessageApplyResult> IngestAvailabilityAsync(string payload)
    {
        if (!protocol.TryDecodeAvailability(payload, out var message) || message is null)
        {
            logger.LogWarning("Rejected malformed restaurant availability message");
            return new(PartnerMessageOutcome.Malformed);
        }

        var result = await repository.ApplyAvailabilityAsync(message, payload, officeTime.UtcNow.UtcDateTime);
        if (result.Outcome == PartnerMessageOutcome.Applied && result.Availability is not null)
        {
            var view = result.Availability;
            await hub.Clients.Group(GroupName(view.LocationId, message.BookingDate)).SendAsync("RestaurantAvailabilityChanged",
                new RestaurantAvailabilityUpdate(
                    view.LocationId,
                    message.BookingDate.ToString("yyyy-MM-dd"),
                    view.RestaurantId,
                    view.Name,
                    view.Capacity,
                    view.AvailableSeats,
                    view.Sequence,
                    view.UpdatedAtUtc,
                    "telegram-mock"));
        }

        var locationId = result.Availability?.LocationId
            ?? (await repository.GetRestaurantAvailabilityAsync("HQ", message.BookingDate)).FirstOrDefault(x => x.RestaurantId == message.RestaurantId)?.LocationId;
        if (!string.IsNullOrWhiteSpace(locationId))
        {
            await hub.Clients.Group(GroupName(locationId!, message.BookingDate)).SendAsync("RestaurantMessageReceived",
                new RestaurantMessageEvent(
                    locationId!,
                    message.BookingDate.ToString("yyyy-MM-dd"),
                    message.RestaurantId,
                    "AVAIL",
                    result.Outcome.ToString().ToLowerInvariant(),
                    message.Sequence,
                    officeTime.UtcNow.UtcDateTime));
        }

        return result;
    }

    public async Task<RestaurantBookingServiceResult> BookAsync(LunchBooking booking, CancellationToken cancellationToken)
    {
        var start = await repository.BeginRestaurantBookingAsync(booking);
        if (!start.Attempt.Succeeded) return new(start.Attempt, "LOCAL_REJECTED", 0, null);

        var pending = start.Attempt.Booking!;
        var command = new PartnerBookingCommand(
            pending.PartnerCorrelationId!,
            pending.RestaurantId!,
            pending.BookingDate,
            pending.SlotId!,
            1,
            start.AvailableBeforeConfirmation,
            pending.MenuItemIds);

        string encodedResult;
        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linked.CancelAfter(officeTime.PartnerPendingTimeout);
            encodedResult = await gateway.SendBookingAsync(command, linked.Token);
        }
        catch (Exception exception) when (exception is OperationCanceledException or TaskCanceledException)
        {
            logger.LogWarning("Restaurant booking timed out for restaurant {RestaurantId}", command.RestaurantId);
            var timedOut = await repository.CompleteRestaurantBookingAsync(command.CorrelationId,
                new(command.CorrelationId, command.RestaurantId, command.BookingDate, "TIMEOUT", start.AvailableBeforeConfirmation, null),
                officeTime.UtcNow.UtcDateTime);
            await BroadcastAvailabilityAsync("HQ", command.BookingDate, command.RestaurantId, "timeout", cancellationToken);
            return new(timedOut, "TIMEOUT", start.AvailableBeforeConfirmation, null);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Restaurant booking failed for restaurant {RestaurantId}", command.RestaurantId);
            var failed = await repository.CompleteRestaurantBookingAsync(command.CorrelationId,
                new(command.CorrelationId, command.RestaurantId, command.BookingDate, "FAILED", start.AvailableBeforeConfirmation, null),
                officeTime.UtcNow.UtcDateTime);
            await BroadcastAvailabilityAsync("HQ", command.BookingDate, command.RestaurantId, "failure", cancellationToken);
            return new(failed, "FAILED", start.AvailableBeforeConfirmation, null);
        }

        if (!protocol.TryDecodeBookingResult(encodedResult, out var partnerResult) || partnerResult is null)
            partnerResult = new(command.CorrelationId, command.RestaurantId, command.BookingDate, "MALFORMED", start.AvailableBeforeConfirmation, null);

        var configuredRestaurant = (await repository.GetRestaurantAvailabilityAsync("HQ", booking.BookingDate))
            .First(x => x.RestaurantId == booking.RestaurantId);
        if (partnerResult.CorrelationId != command.CorrelationId ||
            partnerResult.RestaurantId != command.RestaurantId ||
            partnerResult.BookingDate != command.BookingDate ||
            partnerResult.RemainingSeats > configuredRestaurant.Capacity)
        {
            partnerResult = new(command.CorrelationId, command.RestaurantId, command.BookingDate, "MALFORMED", start.AvailableBeforeConfirmation, null);
        }

        var completed = await repository.CompleteRestaurantBookingAsync(command.CorrelationId, partnerResult, officeTime.UtcNow.UtcDateTime);
        await BroadcastAvailabilityAsync(configuredRestaurant.LocationId, booking.BookingDate, booking.RestaurantId!, "booking-result", cancellationToken);
        return new(completed, partnerResult.Code, partnerResult.RemainingSeats, partnerResult.PartnerReference);
    }

    public async Task<bool> CancelPartnerBookingAsync(LunchBooking booking, CancellationToken cancellationToken = default)
    {
        if (booking.PartnerStatus != PartnerBookingStatus.Confirmed ||
            string.IsNullOrWhiteSpace(booking.RestaurantId) ||
            string.IsNullOrWhiteSpace(booking.SlotId) ||
            string.IsNullOrWhiteSpace(booking.PartnerReference))
        {
            return true;
        }

        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linked.CancelAfter(officeTime.PartnerPendingTimeout);
            var payload = await gateway.SendCancellationAsync(new PartnerCancellationCommand(
                Guid.NewGuid().ToString("N"),
                booking.RestaurantId,
                booking.BookingDate,
                booking.SlotId,
                booking.PartnerReference),
                linked.Token);

            if (!protocol.TryDecodeCancellationResult(payload, out var result) || result is null) return false;
            return result.Confirmed;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Restaurant cancellation failed for booking {BookingId}", booking.BookingId);
            return false;
        }
    }

    public async Task BroadcastAvailabilityAsync(string locationId, DateOnly date, string restaurantId, string source, CancellationToken cancellationToken = default)
    {
        var availability = (await repository.GetRestaurantAvailabilityAsync(locationId, date)).FirstOrDefault(x => x.RestaurantId == restaurantId);
        if (availability is null) return;

        await hub.Clients.Group(GroupName(locationId, date)).SendAsync("RestaurantAvailabilityChanged",
            new RestaurantAvailabilityUpdate(
                availability.LocationId,
                date.ToString("yyyy-MM-dd"),
                availability.RestaurantId,
                availability.Name,
                availability.Capacity,
                availability.AvailableSeats,
                availability.Sequence,
                availability.UpdatedAtUtc,
                source),
            cancellationToken);
    }

    public async Task<IReadOnlyList<RestaurantMessageApplyResult>> TickAsync(string locationId, DateOnly date)
    {
        var restaurants = await repository.GetRestaurantAvailabilityAsync(locationId, date);
        var results = new List<RestaurantMessageApplyResult>(restaurants.Count);
        foreach (var restaurant in restaurants)
        {
            var nextAvailable = restaurant.AvailableSeats > 2 ? restaurant.AvailableSeats - 1 : restaurant.Capacity;
            var message = new PartnerAvailabilityMessage(
                Guid.NewGuid().ToString("N"),
                restaurant.RestaurantId,
                date,
                nextAvailable,
                restaurant.PartnerSequence + 1,
                officeTime.UtcNow.UtcDateTime);
            results.Add(await IngestAvailabilityAsync(protocol.EncodeAvailability(message)));
        }

        return results;
    }

    public Task<RestaurantMessageApplyResult> SimulateAvailabilityAsync(string restaurantId, DateOnly date, int availableSeats)
    {
        return SimulateAsync();

        async Task<RestaurantMessageApplyResult> SimulateAsync()
        {
            var current = (await repository.GetRestaurantAvailabilityAsync("HQ", date)).FirstOrDefault(x => x.RestaurantId == restaurantId);
            var sequence = (current?.PartnerSequence ?? 0) + 1;
            var message = new PartnerAvailabilityMessage(Guid.NewGuid().ToString("N"), restaurantId, date, availableSeats, sequence, officeTime.UtcNow.UtcDateTime);
            return await IngestAvailabilityAsync(protocol.EncodeAvailability(message));
        }
    }

    private static string GroupName(string locationId, DateOnly date) => $"availability:{locationId}:{date:yyyy-MM-dd}";
}
