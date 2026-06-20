using Microsoft.AspNetCore.SignalR;
using Spotly.Api.Dtos;

namespace Spotly.Api.Hubs;

public class AvailabilityHub : Hub
{
    /// <summary>Client calls this to subscribe to a sede/date availability group.</summary>
    public async Task JoinGroup(string sedeId, string date) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, $"availability:{sedeId}:{date}");

    /// <summary>Client calls this to unsubscribe.</summary>
    public async Task LeaveGroup(string sedeId, string date) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"availability:{sedeId}:{date}");
}

public static class AvailabilityHubExtensions
{
    public static async Task NotifyStatusChangeAsync(
        IHubContext<AvailabilityHub> hub,
        string sedeId,
        string date,
        AvailabilityUpdate update)
    {
        await hub.Clients.Group($"availability:{sedeId}:{date}")
            .SendAsync("ResourceStatusChanged", update);
    }
}
