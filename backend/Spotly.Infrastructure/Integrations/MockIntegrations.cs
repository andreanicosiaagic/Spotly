using Microsoft.Extensions.Logging;
using Spotly.Domain.Interfaces;

namespace Spotly.Infrastructure.Integrations;

public class MockCalendarIntegration(ILogger<MockCalendarIntegration> logger) : ICalendarIntegration
{
    public Task CreateEventAsync(string userId, string title, DateOnly date, string description)
    {
        logger.LogInformation("[MOCK Calendar] Event creation simulated for date={Date}", date);
        return Task.CompletedTask;
    }
}

public class MockAccessControlSystem(ILogger<MockAccessControlSystem> logger) : IAccessControlSystem
{
    public Task<bool> ValidateCheckInAsync(string userId, string resourceId)
    {
        logger.LogInformation("[MOCK AccessControl] Check-in validation simulated");
        return Task.FromResult(true);
    }
}

public class MockRestaurantPartner(ILogger<MockRestaurantPartner> logger) : IRestaurantPartner
{
    public Task<bool> ConfirmOrderAsync(string restaurantId, string slotId, string userId)
    {
        logger.LogInformation("[MOCK RestaurantPartner] Order confirmation simulated");
        return Task.FromResult(true);
    }
}

public class MockWelfareProvider(ILogger<MockWelfareProvider> logger) : IWelfareProvider
{
    public Task<decimal> GetAvailableBudgetAsync(string userId)
    {
        logger.LogInformation("[MOCK Welfare] Budget lookup simulated");
        return Task.FromResult(999.99m);
    }

    public Task<bool> DeductAsync(string userId, decimal amount)
    {
        logger.LogInformation("[MOCK Welfare] Budget deduction simulated amount={Amount}", amount);
        return Task.FromResult(true);
    }
}

public class MockNotificationService(ILogger<MockNotificationService> logger) : INotificationService
{
    public Task SendAsync(string userId, string subject, string body)
    {
        logger.LogInformation("[MOCK Notification] Notification delivery simulated");
        return Task.CompletedTask;
    }
}
