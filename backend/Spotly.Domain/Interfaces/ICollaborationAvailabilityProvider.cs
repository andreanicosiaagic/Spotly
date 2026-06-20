using Spotly.Domain.Entities;

namespace Spotly.Domain.Interfaces;

public interface ICollaborationAvailabilityProvider
{
    Task<CollaborationAvailability?> GetUserAvailabilityAsync(string userId, DateOnly date, TimeOnly fromUtc, TimeOnly toUtc);
    Task<IReadOnlyList<CollaborationAvailability>> GetTeamAvailabilityAsync(string managerUserId, DateOnly date, TimeOnly fromUtc, TimeOnly toUtc);
}
