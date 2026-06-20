using Microsoft.Extensions.Logging;
using Spotly.Domain.Entities;
using Spotly.Domain.Interfaces;

namespace Spotly.Infrastructure.Integrations;

public sealed class MockTelegramRestaurantGateway(
    IRestaurantPartnerProtocol protocol,
    ILogger<MockTelegramRestaurantGateway> logger) : IRestaurantMessagingGateway, IRestaurantDemoGateway
{
    private static readonly HashSet<string> SupportedCodes = ["OK", "FULL", "CLOSED", "INVALID_DATE", "TIMEOUT"];
    private readonly Dictionary<string, string> _nextOutcomes = [];
    private readonly Lock _gate = new();

    public Task<string> SendBookingAsync(PartnerBookingCommand command, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var requestPayload = protocol.EncodeBooking(command);
        string code;
        lock (_gate)
        {
            code = _nextOutcomes.Remove(command.RestaurantId, out var configured) ? configured : "OK";
        }
        var remaining = code == "OK" ? Math.Max(0, command.AvailableBeforeConfirmation - command.PartySize)
            : code == "FULL" ? 0 : command.AvailableBeforeConfirmation;
        var reference = code == "OK" ? $"TG-{Guid.NewGuid():N}"[..15] : null;
        logger.LogInformation("[MOCK Telegram] Protocol booking sent bytes={PayloadLength}; response code={PartnerCode} remaining={RemainingSeats}",
            requestPayload.Length, code, remaining);
        return Task.FromResult(protocol.EncodeBookingResult(new PartnerBookingResult(command.CorrelationId, command.RestaurantId,
            command.BookingDate, code, remaining, reference)));
    }

    public void SetNextBookingOutcome(string restaurantId, string code)
    {
        var normalized = code.ToUpperInvariant();
        if (!SupportedCodes.Contains(normalized)) throw new ArgumentOutOfRangeException(nameof(code), code, "Unsupported demo outcome.");
        lock (_gate) _nextOutcomes[restaurantId] = normalized;
    }
}
