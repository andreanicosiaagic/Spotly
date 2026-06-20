# Matching team da Entra, Teams work location e calendario

## Problem Statement

Spotly deve aiutare un Manager a capire quali membri del team possono essere presenti nello stesso giorno e nella stessa sede. Nell'app finale identità, membership, work location Teams e free/busy Outlook arriveranno da Entra ID e Microsoft Graph; nella demo ogni dato deve essere mockato e deterministico.

## Solution

Introdurre un provider di collaborazione indipendente da Graph che restituisce membri opt-in, work location e sole finestre free/busy. Un servizio di dominio confronta la sede Teams dell'utente corrente con quella dei membri e la disponibilità del calendario nell'intervallo lavorativo. Un endpoint Manager restituisce il risultato minimizzato e la dashboard demo lo visualizza.

## User Stories

1. As a Manager, I want to see the team members working at my office location, so that I can coordinate an office day.
2. As a Manager, I want calendar free/busy included in the match, so that I do not propose a time when colleagues are unavailable.
3. As a Manager, I want to distinguish free, tentative, busy, and out-of-office, so that recommendations remain explainable.
4. As a Manager, I want to know when Teams work location is missing, so that uncertain data is not treated as a match.
5. As a Manager, I want only opt-in members displayed, so that presence privacy is respected.
6. As a Dipendente, I want my own work-location summary without access to team details, so that RBAC is preserved.
7. As an operator, I want no event titles or meeting attendees exposed, so that calendar data is minimized.
8. As a developer, I want deterministic mock users, work locations, and calendars, so that the demo has no Graph side effects.
9. As a future integrator, I want one provider interface for Entra and Graph, so that the mock can be replaced without changing matching logic.
10. As an operator, I want no names, emails, locations, or calendar data in technical logs, so that PII is not leaked.

## Implementation Decisions

- `ICollaborationAvailabilityProvider` is the only boundary for directory membership, Teams work location, and calendar free/busy.
- Demo implementation is hardcoded mock data; it never calls Entra, Teams, Outlook, or Graph.
- Final implementation will use Entra object IDs as stable identifiers and Graph schedule/work-location APIs behind the same interface.
- Matching interval defaults to 09:00–17:00 UTC for the selected day and is explicit in the response.
- A member matches when opt-in is true, work mode is Office, location ID equals the current user's Teams location, and calendar status is Free or Tentative.
- Busy and OutOfOffice members are visible to Managers but never marked as matches.
- Event subject, organizer, attendees, and meeting location are never requested or returned.
- Manager/Facility/Admin receive member-level results; ordinary employees receive only their own summary.
- API results use display names solely for authorized UI display. Technical logging uses aggregate counts only.

## Testing Decisions

- Primary seam is the HTTP endpoint with mock authentication headers.
- Test Manager access, employee denial for team details, exact matching rules, missing work location, opt-out filtering, and absence of event metadata.
- Frontend build verifies DTO integration; browser visual testing is not repeated unless layout behavior changes materially.

## Out of Scope

Real Graph permissions, app registration, admin consent, Teams notifications, calendar writes, event content, and automatic office-day booking are excluded from this demo.

## Further Notes

Recommended future Graph permissions should use least privilege and application/delegated mode appropriate to the deployment, but no permission is introduced in this POC.
