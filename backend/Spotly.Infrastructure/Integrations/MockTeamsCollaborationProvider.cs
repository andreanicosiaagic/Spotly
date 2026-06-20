using Spotly.Domain.Entities;
using Spotly.Domain.Interfaces;

namespace Spotly.Infrastructure.Integrations;

public sealed class MockTeamsCollaborationProvider : ICollaborationAvailabilityProvider
{
    private static readonly IReadOnlyList<CollaborationAvailability> Members =
    [
        new("u1", "Giulia Romano", true, WorkMode.Office, "HQ", "Milano HQ", CalendarAvailability.Free),
        new("u2", "Marco Bianchi", true, WorkMode.Office, "HQ", "Milano HQ", CalendarAvailability.Free),
        new("u3", "Sara Conti", true, WorkMode.Remote, null, "Da remoto", CalendarAvailability.Free),
        new("u4", "Luca Ferri", true, WorkMode.Office, "HQ", "Milano HQ", CalendarAvailability.Busy),
        new("u5", "Elena Greco", true, WorkMode.Office, "ROMA", "Roma EUR", CalendarAvailability.Free),
        new("u6", "Paolo Riva", true, WorkMode.Office, "HQ", "Milano HQ", CalendarAvailability.Tentative),
        new("u7", "Membro riservato", false, WorkMode.Office, "HQ", "Milano HQ", CalendarAvailability.Free),
    ];

    public Task<CollaborationAvailability?> GetUserAvailabilityAsync(string userId, DateOnly date, TimeOnly fromUtc, TimeOnly toUtc) =>
        Task.FromResult(Members.FirstOrDefault(x => x.UserId == userId));

    public Task<IReadOnlyList<CollaborationAvailability>> GetTeamAvailabilityAsync(string managerUserId, DateOnly date,
        TimeOnly fromUtc, TimeOnly toUtc) => Task.FromResult(Members);
}
