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
    TimeProvider clock,
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
        var result = await repository.ApplyAvailabilityAsync(message, payload, clock.GetUtcNow().UtcDateTime);
        if (result.Outcome == PartnerMessageOutcome.Applied && result.Availability is not null)
        {
            var view = result.Availability;
            await hub.Clients.Group($"availability:HQ:{message.BookingDate:yyyy-MM-dd}").SendAsync("RestaurantAvailabilityChanged",
                new RestaurantAvailabilityUpdate(view.RestaurantId, view.Name, view.Capacity, view.AvailableSeats,
                    view.Sequence, view.UpdatedAtUtc, "telegram-mock"));
        }
        await hub.Clients.All.SendAsync("RestaurantMessageReceived", new RestaurantMessageEvent(message.RestaurantId, "AVAIL",
            result.Outcome.ToString().ToLowerInvariant(), message.Sequence, clock.GetUtcNow().UtcDateTime));
        return result;
    }

    public async Task<RestaurantBookingServiceResult> BookAsync(LunchBooking booking, CancellationToken cancellationToken)
    {
        var start = await repository.BeginRestaurantBookingAsync(booking);
        if (!start.Attempt.Succeeded) return new(start.Attempt, "LOCAL_REJECTED", 0, null);
        var pending = start.Attempt.Booking!;
        var command = new PartnerBookingCommand(pending.PartnerCorrelationId!, pending.RestaurantId!, pending.BookingDate, 1,
            start.AvailableBeforeConfirmation);
        var encodedResult = await gateway.SendBookingAsync(command, cancellationToken);
        if (!protocol.TryDecodeBookingResult(encodedResult, out var partnerResult) || partnerResult is null)
            partnerResult = new(command.CorrelationId, command.RestaurantId, command.BookingDate, "MALFORMED",
                start.AvailableBeforeConfirmation, null);
        var configuredRestaurant = (await repository.GetRestaurantAvailabilityAsync("HQ", booking.BookingDate))
            .First(x => x.RestaurantId == booking.RestaurantId);
        if (partnerResult.CorrelationId != command.CorrelationId || partnerResult.RestaurantId != command.RestaurantId ||
            partnerResult.BookingDate != command.BookingDate || partnerResult.RemainingSeats > configuredRestaurant.Capacity)
            partnerResult = new(command.CorrelationId, command.RestaurantId, command.BookingDate, "MALFORMED",
                start.AvailableBeforeConfirmation, null);
        var completed = await repository.CompleteRestaurantBookingAsync(command.CorrelationId, partnerResult, clock.GetUtcNow().UtcDateTime);
        var availability = (await repository.GetRestaurantAvailabilityAsync("HQ", booking.BookingDate))
            .FirstOrDefault(x => x.RestaurantId == booking.RestaurantId);
        if (availability is not null)
            await hub.Clients.Group($"availability:HQ:{booking.BookingDate:yyyy-MM-dd}").SendAsync("RestaurantAvailabilityChanged",
                new RestaurantAvailabilityUpdate(availability.RestaurantId, availability.Name, availability.Capacity,
                    availability.AvailableSeats, availability.Sequence, availability.UpdatedAtUtc, "booking-result"), cancellationToken);
        return new(completed, partnerResult.Code, partnerResult.RemainingSeats, partnerResult.PartnerReference);
    }

    public async Task<IReadOnlyList<RestaurantMessageApplyResult>> TickAsync(string locationId, DateOnly date)
    {
        var restaurants = await repository.GetRestaurantAvailabilityAsync(locationId, date);
        var results = new List<RestaurantMessageApplyResult>(restaurants.Count);
        foreach (var restaurant in restaurants)
        {
            var nextAvailable = restaurant.AvailableSeats > 2 ? restaurant.AvailableSeats - 1 : restaurant.Capacity;
            var message = new PartnerAvailabilityMessage(Guid.NewGuid().ToString("N"), restaurant.RestaurantId, date,
                nextAvailable, restaurant.Sequence + 1, clock.GetUtcNow().UtcDateTime);
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
            var sequence = (current?.Sequence ?? 0) + 1;
            var message = new PartnerAvailabilityMessage(Guid.NewGuid().ToString("N"), restaurantId, date, availableSeats,
                sequence, clock.GetUtcNow().UtcDateTime);
            return await IngestAvailabilityAsync(protocol.EncodeAvailability(message));
        }
    }
}
