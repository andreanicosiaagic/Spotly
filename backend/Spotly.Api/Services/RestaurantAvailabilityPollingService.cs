namespace Spotly.Api.Services;

public sealed class RestaurantAvailabilityPollingService(
    IServiceScopeFactory scopeFactory,
    TimeProvider clock,
    IConfiguration configuration,
    ILogger<RestaurantAvailabilityPollingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(configuration.GetValue("RestaurantMessaging:PollingIntervalMinutes", 10));
        using var timer = new PeriodicTimer(interval, clock);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var live = scope.ServiceProvider.GetRequiredService<RestaurantLiveService>();
            var date = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            var results = await live.TickAsync("HQ", date);
            logger.LogInformation("Processed {RestaurantUpdateCount} scheduled restaurant availability updates", results.Count);
        }
    }
}
