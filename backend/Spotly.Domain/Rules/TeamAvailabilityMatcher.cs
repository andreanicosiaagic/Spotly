using Spotly.Domain.Entities;

namespace Spotly.Domain.Rules;

public static class TeamAvailabilityMatcher
{
    public static TeamAvailabilityMatch Match(DateOnly date, TimeOnly fromUtc, TimeOnly toUtc,
        CollaborationAvailability currentUser, IEnumerable<CollaborationAvailability> members)
    {
        var visibleMembers = members.Where(x => x.PresenceVisibilityOptIn && x.UserId != currentUser.UserId)
            .Select(member => ToMatch(currentUser, member)).OrderByDescending(x => x.IsMatch).ThenBy(x => x.DisplayName).ToArray();
        return new(date, fromUtc, toUtc, currentUser.LocationId, currentUser.LocationLabel,
            visibleMembers.Count(x => x.IsMatch), visibleMembers);
    }

    private static TeamMemberMatch ToMatch(CollaborationAvailability currentUser, CollaborationAvailability member)
    {
        var sameOffice = currentUser.WorkMode == WorkMode.Office && member.WorkMode == WorkMode.Office &&
            currentUser.LocationId is not null && currentUser.LocationId == member.LocationId;
        var calendarCompatible = member.CalendarStatus is CalendarAvailability.Free or CalendarAvailability.Tentative;
        var isMatch = sameOffice && calendarCompatible;
        var reason = member.WorkMode != WorkMode.Office ? "Non lavora in sede"
            : member.LocationId is null ? "Sede Teams non impostata"
            : !sameOffice ? "Sede Teams diversa"
            : member.CalendarStatus == CalendarAvailability.Busy ? "Calendario occupato"
            : member.CalendarStatus == CalendarAvailability.OutOfOffice ? "Fuori ufficio"
            : member.CalendarStatus == CalendarAvailability.Unknown ? "Calendario non disponibile"
            : member.CalendarStatus == CalendarAvailability.Tentative ? "Stessa sede, disponibilità provvisoria"
            : "Stessa sede e calendario libero";
        return new(member.UserId, member.DisplayName, member.WorkMode, member.LocationId, member.LocationLabel,
            member.CalendarStatus, isMatch, reason);
    }
}
