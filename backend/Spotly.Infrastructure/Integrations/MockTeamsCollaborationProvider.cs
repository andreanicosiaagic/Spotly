using Spotly.Domain.Entities;
using Spotly.Domain.Interfaces;

namespace Spotly.Infrastructure.Integrations;

public sealed class MockTeamsCollaborationProvider : ICollaborationAvailabilityProvider
{
    private static readonly IReadOnlyDictionary<string, CollaborationAvailability> BaseMembers = new Dictionary<string, CollaborationAvailability>
    {
        ["u1"] = new("u1", "Giulia Romano", true, WorkMode.Office, "HQ", "Milano HQ", CalendarAvailability.Free),
        ["u2"] = new("u2", "Marco Bianchi", true, WorkMode.Office, "HQ", "Milano HQ", CalendarAvailability.Free),
        ["u3"] = new("u3", "Sara Conti", true, WorkMode.Remote, null, "Da remoto", CalendarAvailability.Free),
        ["u4"] = new("u4", "Luca Ferri", true, WorkMode.Office, "HQ", "Milano HQ", CalendarAvailability.Busy),
        ["u5"] = new("u5", "Elena Greco", true, WorkMode.Office, "ROMA", "Roma EUR", CalendarAvailability.Free),
        ["u6"] = new("u6", "Paolo Riva", true, WorkMode.Office, "HQ", "Milano HQ", CalendarAvailability.Tentative),
        ["u7"] = new("u7", "Membro riservato", false, WorkMode.Office, "HQ", "Milano HQ", CalendarAvailability.Free),
    };

    public Task<CollaborationAvailability?> GetUserAvailabilityAsync(string userId, DateOnly date, TimeOnly fromUtc, TimeOnly toUtc)
    {
        if (!BaseMembers.TryGetValue(userId, out var member)) return Task.FromResult<CollaborationAvailability?>(null);
        return Task.FromResult<CollaborationAvailability?>(VariantFor(member, date, fromUtc, toUtc));
    }

    public Task<IReadOnlyList<CollaborationAvailability>> GetTeamAvailabilityAsync(string managerUserId, DateOnly date, TimeOnly fromUtc, TimeOnly toUtc) =>
        Task.FromResult<IReadOnlyList<CollaborationAvailability>>(BaseMembers.Values.Select(member => VariantFor(member, date, fromUtc, toUtc)).ToArray());

    private static CollaborationAvailability VariantFor(CollaborationAvailability member, DateOnly date, TimeOnly fromUtc, TimeOnly toUtc)
    {
        var seed = (date.DayNumber + fromUtc.Hour + toUtc.Hour + member.UserId[1]) % 6;
        return member.UserId switch
        {
            "u1" => member with { CalendarStatus = seed % 2 == 0 ? CalendarAvailability.Free : CalendarAvailability.Tentative },
            "u2" => member with { CalendarStatus = CalendarAvailability.Free },
            "u3" => member with { WorkMode = WorkMode.Remote, LocationId = null, LocationLabel = "Da remoto", CalendarStatus = CalendarAvailability.Free },
            "u4" => member with { CalendarStatus = seed % 2 == 0 ? CalendarAvailability.Busy : CalendarAvailability.OutOfOffice },
            "u5" => member with { LocationId = seed % 3 == 0 ? "HQ" : "ROMA", LocationLabel = seed % 3 == 0 ? "Milano HQ" : "Roma EUR" },
            "u6" => member with { CalendarStatus = seed % 2 == 0 ? CalendarAvailability.Tentative : CalendarAvailability.Free },
            _ => member,
        };
    }
}
