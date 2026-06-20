using System.Globalization;

namespace Spotly.Api.Services;

public sealed record CheckInWindow(DateTime OpensAtUtc, DateTime ClosesAtUtc);

public sealed class OfficeTime(IConfiguration configuration, TimeProvider clock)
{
    private readonly Lazy<TimeZoneInfo> _timeZone = new(() => ResolveTimeZone(configuration["Site:TimeZoneId"] ?? "Europe/Rome"));

    public TimeZoneInfo TimeZone => _timeZone.Value;

    public DateTimeOffset UtcNow => clock.GetUtcNow();

    public DateTime LocalNow => TimeZoneInfo.ConvertTime(UtcNow, TimeZone).DateTime;

    public DateOnly Today => DateOnly.FromDateTime(LocalNow);

    public TimeOnly CheckInOpensLocal => ParseTime(configuration["Booking:CheckInOpenLocal"], new TimeOnly(7, 0));

    public TimeOnly CheckInClosesLocal => ParseTime(configuration["Booking:CheckInCloseLocal"], new TimeOnly(9, 30));

    public TimeOnly LunchServiceStartsLocal => ParseTime(configuration["Lunch:ServiceStartLocal"], new TimeOnly(11, 0));

    public TimeOnly LunchServiceEndsLocal => ParseTime(configuration["Lunch:ServiceEndLocal"], new TimeOnly(15, 0));

    public TimeSpan PartnerPendingTimeout => TimeSpan.FromSeconds(configuration.GetValue("RestaurantMessaging:PartnerTimeoutSeconds", 30));

    public CheckInWindow GetCheckInWindow(DateOnly bookingDate) => new(ToUtc(bookingDate, CheckInOpensLocal), ToUtc(bookingDate, CheckInClosesLocal));

    public DateTime ToUtc(DateOnly date, TimeOnly localTime)
    {
        var localDateTime = DateTime.SpecifyKind(date.ToDateTime(localTime), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(localDateTime, TimeZone);
    }

    public bool IsOutsideLunchServiceWindow()
    {
        var now = TimeOnly.FromDateTime(LocalNow);
        return now < LunchServiceStartsLocal || now >= LunchServiceEndsLocal;
    }

    private static TimeOnly ParseTime(string? value, TimeOnly fallback) =>
        TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : fallback;

    private static TimeZoneInfo ResolveTimeZone(string configuredId)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(configuredId); }
        catch (TimeZoneNotFoundException) when (configuredId == "Europe/Rome")
        {
            return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        }
        catch (TimeZoneNotFoundException) when (configuredId == "W. Europe Standard Time")
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");
        }
    }
}
