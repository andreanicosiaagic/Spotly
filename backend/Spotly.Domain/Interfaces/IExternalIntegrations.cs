namespace Spotly.Domain.Interfaces;

// Mock integrations — real connectors registered in prod DI

public interface ICalendarIntegration
{
    Task CreateEventAsync(string userId, string title, DateOnly date, string description);
}

public interface IAccessControlSystem
{
    Task<bool> ValidateCheckInAsync(string userId, string resourceId);
}

public interface IRestaurantPartner
{
    Task<bool> ConfirmOrderAsync(string restaurantId, string slotId, string userId);
}

public interface IWelfareProvider
{
    Task<decimal> GetAvailableBudgetAsync(string userId);
    Task<bool> DeductAsync(string userId, decimal amount);
}

public interface INotificationService
{
    Task SendAsync(string userId, string subject, string body);
}
