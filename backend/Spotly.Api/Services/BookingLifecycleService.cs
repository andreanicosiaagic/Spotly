using Microsoft.AspNetCore.SignalR;
using Spotly.Api.Dtos;
using Spotly.Api.Hubs;
using Spotly.Domain.Interfaces;

namespace Spotly.Api.Services;

public sealed class BookingLifecycleService(
    IParkingRepository parking,
    IDeskRepository desks,
    IHubContext<AvailabilityHub> hub,
    TimeProvider clock,
    ILogger<BookingLifecycleService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30), clock);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var now = clock.GetUtcNow().UtcDateTime;
            var parkingReleased = (await parking.ReleaseExpiredLocksAsync(now)).Concat(await parking.ReleaseNoShowsAsync(now));
            var desksReleased = (await desks.ReleaseExpiredLocksAsync(now)).Concat(await desks.ReleaseNoShowsAsync(now));
            var releasedCount = 0;
            foreach (var item in parkingReleased)
            {
                releasedCount++;
                await AvailabilityHubExtensions.NotifyStatusChangeAsync(hub, "HQ", item.BookingDate.ToString("yyyy-MM-dd"),
                    new AvailabilityUpdate(item.ResourceId, "parking", "available"));
            }
            foreach (var item in desksReleased)
            {
                releasedCount++;
                await AvailabilityHubExtensions.NotifyStatusChangeAsync(hub, "HQ", item.BookingDate.ToString("yyyy-MM-dd"),
                    new AvailabilityUpdate(item.ResourceId, "desk", "available"));
            }
            if (releasedCount > 0) logger.LogInformation("Released {ReleasedResourceCount} expired Spotly resources", releasedCount);
        }
    }
}
