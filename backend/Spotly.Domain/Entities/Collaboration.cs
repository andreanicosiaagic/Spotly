namespace Spotly.Domain.Entities;

public enum WorkMode { Office, Remote, Unknown }
public enum CalendarAvailability { Free, Tentative, Busy, OutOfOffice, Unknown }

public record CollaborationAvailability(
    string UserId,
    string DisplayName,
    bool PresenceVisibilityOptIn,
    WorkMode WorkMode,
    string? LocationId,
    string? LocationLabel,
    CalendarAvailability CalendarStatus);

public record TeamMemberMatch(
    string UserId,
    string DisplayName,
    WorkMode WorkMode,
    string? LocationId,
    string? LocationLabel,
    CalendarAvailability CalendarStatus,
    bool IsMatch,
    string Reason);

public record TeamAvailabilityMatch(
    DateOnly Date,
    TimeOnly WindowStartUtc,
    TimeOnly WindowEndUtc,
    string? CurrentLocationId,
    string? CurrentLocationLabel,
    int MatchingMembers,
    IReadOnlyList<TeamMemberMatch> Members);
