using Microsoft.Extensions.Logging;
using Spotly.Domain.Interfaces;

namespace Spotly.Infrastructure.Integrations;

public class MockCalendarIntegration(ILogger<MockCalendarIntegration> logger) : ICalendarIntegration
{
    public Task CreateEventAsync(string userId, string title, DateOnly date, string description)
    {
        logger.LogInformation("[MOCK Calendar] CreateEvent userId={UserId} title={Title} date={Date}", userId, title, date);
        return Task.CompletedTask;
    }
}

public class MockAccessControlSystem(ILogger<MockAccessControlSystem> logger) : IAccessControlSystem
{
    public Task<bool> ValidateCheckInAsync(string userId, string resourceId)
    {
        logger.LogInformation("[MOCK AccessControl] CheckIn userId={UserId} resourceId={ResourceId} → OK", userId, resourceId);
        return Task.FromResult(true);
    }
}

public class MockRestaurantPartner(ILogger<MockRestaurantPartner> logger) : IRestaurantPartner
{
    public Task<bool> ConfirmOrderAsync(string restaurantId, string slotId, string userId)
    {
        logger.LogInformation("[MOCK RestaurantPartner] ConfirmOrder restaurantId={RestaurantId} slotId={SlotId}", restaurantId, slotId);
        return Task.FromResult(true);
    }
}

public class MockWelfareProvider(ILogger<MockWelfareProvider> logger) : IWelfareProvider
{
    public Task<decimal> GetAvailableBudgetAsync(string userId)
    {
        logger.LogInformation("[MOCK Welfare] GetBudget userId={UserId} → 999.99", userId);
        return Task.FromResult(999.99m);
    }

    public Task<bool> DeductAsync(string userId, decimal amount)
    {
        logger.LogInformation("[MOCK Welfare] Deduct userId={UserId} amount={Amount} → OK", userId, amount);
        return Task.FromResult(true);
    }
}

public class MockNotificationService(ILogger<MockNotificationService> logger) : INotificationService
{
    public Task SendAsync(string userId, string subject, string body)
    {
        logger.LogInformation("[MOCK Notification] To={UserId} Subject={Subject}", userId, subject);
        return Task.CompletedTask;
    }
}
