using Spotly.Domain.Entities;

namespace Spotly.Domain.Interfaces;

public interface IRestaurantMessagingGateway
{
    Task<string> SendBookingAsync(PartnerBookingCommand command, CancellationToken cancellationToken = default);
}

public interface IRestaurantPartnerProtocol
{
    string EncodeAvailability(PartnerAvailabilityMessage message);
    bool TryDecodeAvailability(string payload, out PartnerAvailabilityMessage? message);
    string EncodeBooking(PartnerBookingCommand command);
    string EncodeBookingResult(PartnerBookingResult result);
    bool TryDecodeBookingResult(string payload, out PartnerBookingResult? result);
}

public interface IRestaurantDemoGateway
{
    void SetNextBookingOutcome(string restaurantId, string code);
}
