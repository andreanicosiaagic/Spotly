using System.Globalization;
using Spotly.Domain.Entities;
using Spotly.Domain.Interfaces;

namespace Spotly.Infrastructure.Integrations;

public sealed class RestaurantPartnerProtocol : IRestaurantPartnerProtocol
{
    private const string Prefix = "SPOTLY|1";

    public string EncodeAvailability(PartnerAvailabilityMessage message) => string.Join('|',
        Prefix, "AVAIL", message.MessageId, message.RestaurantId, message.BookingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        message.AvailableSeats, message.Sequence, message.SentAtUtc.ToString("O", CultureInfo.InvariantCulture));

    public bool TryDecodeAvailability(string payload, out PartnerAvailabilityMessage? message)
    {
        message = null;
        var parts = payload.Split('|', StringSplitOptions.TrimEntries);
        if (parts.Length != 9 || parts[0] != "SPOTLY" || parts[1] != "1" || parts[2] != "AVAIL" ||
            string.IsNullOrWhiteSpace(parts[3]) || string.IsNullOrWhiteSpace(parts[4]) ||
            !DateOnly.TryParseExact(parts[5], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
            !int.TryParse(parts[6], CultureInfo.InvariantCulture, out var available) || available < 0 ||
            !long.TryParse(parts[7], CultureInfo.InvariantCulture, out var sequence) || sequence < 1 ||
            !DateTime.TryParse(parts[8], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var sentAt)) return false;
        message = new(parts[3], parts[4], date, available, sequence, sentAt.ToUniversalTime());
        return true;
    }

    public string EncodeBooking(PartnerBookingCommand command) => string.Join('|', Prefix, "BOOK", command.CorrelationId,
        command.RestaurantId, command.BookingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), command.PartySize);

    public string EncodeBookingResult(PartnerBookingResult result) => string.Join('|', Prefix, "BOOK_RESULT", result.CorrelationId,
        result.RestaurantId, result.BookingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), result.Code,
        result.RemainingSeats, result.PartnerReference ?? "-");

    public bool TryDecodeBookingResult(string payload, out PartnerBookingResult? result)
    {
        result = null;
        var parts = payload.Split('|', StringSplitOptions.TrimEntries);
        if (parts.Length != 9 || parts[0] != "SPOTLY" || parts[1] != "1" || parts[2] != "BOOK_RESULT" ||
            !DateOnly.TryParseExact(parts[5], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
            !int.TryParse(parts[7], CultureInfo.InvariantCulture, out var remaining) || remaining < 0) return false;
        result = new(parts[3], parts[4], date, parts[6], remaining, parts[8] == "-" ? null : parts[8]);
        return true;
    }
}
