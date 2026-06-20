using Spotly.Domain.Rules;
using Spotly.Domain.Entities;

namespace Spotly.Api.Services;

public sealed class RestaurantAvailabilityPollingService(
    IServiceScopeFactory scopeFactory,
    OfficeTime officeTime,
    IConfiguration configuration,
    ILogger<RestaurantAvailabilityPollingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(configuration.GetValue("RestaurantMessaging:PollingIntervalMinutes", 10));
        using var timer = new PeriodicTimer(interval, TimeProvider.System);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var live = scope.ServiceProvider.GetRequiredService<RestaurantLiveService>();
                var applied = 0;
                for (var offset = 0; offset <= BookingRules.MaxBookingWindowDays; offset++)
                {
                    var date = officeTime.Today.AddDays(offset);
                    applied += (await live.TickAsync("HQ", date)).Count(x => x.Outcome == PartnerMessageOutcome.Applied);
                }

                logger.LogInformation("Processed {RestaurantUpdateCount} scheduled restaurant availability updates", applied);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Scheduled restaurant polling tick failed");
            }
        }
    }
}
