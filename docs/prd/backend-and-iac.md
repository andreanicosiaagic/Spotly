# Backend completo e IaC Azure per Spotly

## Problem Statement

Spotly dispone di uno scaffold API per parcheggio, postazioni e pranzo, ma non espone ancora un backend POC completo e deployabile: autenticazione e RBAC non sono applicati uniformemente, diverse regole R-01–R-09 sono vulnerabili a race condition o mancanti, il lifecycle lock/check-in/no-show è incompleto e non esistono artifact IaC ripetibili.

## Solution

Completare il backend ASP.NET Core 10 Minimal API con autenticazione mock locale e Easy Auth in Azure, autorizzazione per ruolo, validazione FluentValidation, persistenza EF Core InMemory, operazioni atomiche di prenotazione, cancellazione, lock e check-in, release automatica, SignalR e integrazioni esterne mock. Preparare un deployment AZD+Bicep su Azure App Service con SignalR e osservabilità.

## User Stories

1. As a Dipendente, I want to view availability by date, so that I can choose a resource.
2. As a Dipendente, I want to book one parking spot per day, so that I can commute to the office.
3. As a Dipendente, I want to book one desk per day independently of parking, so that paired booking remains optional.
4. As a Dipendente, I want to reserve a restaurant slot, so that I can plan lunch.
5. As a Dipendente, I want a lunch box only when restaurants are full or outside operating hours, so that fallback policy is respected.
6. As an eligible Dipendente, I want to book EV, disabled, or guest parking, so that special spaces are protected.
7. As a Dipendente, I want an atomic reservation outcome, so that another user cannot receive the same resource.
8. As a Dipendente, I want to cancel my own booking, so that the resource becomes available.
9. As a Dipendente, I want late cancellation recorded as no-show, so that policy remains enforceable.
10. As a Dipendente, I want to check in, so that my booking is not automatically released.
11. As a connected user, I want realtime availability events, so that maps update without refresh.
12. As a Manager, I want employee capabilities plus protected team endpoints, so that team workflows can be added safely.
13. As Facility, I want protected operational visibility, so that resources and policies are not exposed to ordinary employees.
14. As Admin, I want full authorized access, so that tenant configuration remains controlled.
15. As an operator, I want expired locks and no-shows released automatically, so that inventory is not stranded.
16. As an operator, I want structured logs without domain PII, so that diagnostics do not leak identities.
17. As a developer, I want deterministic mock integrations, so that the POC never calls external systems.
18. As a developer, I want OpenAPI and health endpoints, so that the API can be tested and monitored.
19. As a deployer, I want parameterized Bicep and AZD configuration, so that environments are reproducible without hardcoded secrets.
20. As a deployer, I want HTTPS, TLS 1.2+, managed identity, Easy Auth, and telemetry configured by IaC, so that the POC has secure defaults.

## Implementation Decisions

- Preserve modules M0 Auth, M1 Parking, M2 Desk, and M3 Lunch as Minimal API endpoint groups.
- Treat the authenticated claim as the sole user identity; request bodies cannot impersonate another user.
- Use explicit authorization policies for Dipendente, Manager, Facility, and Admin role hierarchies.
- Use EF Core 10 InMemory behind repository interfaces; all external integrations remain mocked.
- Perform R-01 and R-03 checks inside one repository critical section. Expose explicit temporary locks and atomic direct booking for the existing frontend contract.
- Track check-in deadline and check-in timestamp; a hosted lifecycle service releases expired locks and marks overdue bookings no-show.
- Apply R-09 during cancellation and return whether the outcome is cancelled or no-show.
- Enforce special parking eligibility from claims and department-reserved desk quota before persistence.
- Keep parking and desk workflows independent; pairing may be suggested but is never required.
- Emit minimal SignalR payloads without user identifiers.
- Use AZD with modular Bicep targeting Linux App Service, Azure SignalR Free, Log Analytics, and Application Insights.
- Keep subscription, region, tenant, and Entra client identifiers as deployment parameters; do not store credentials in source.

## Testing Decisions

- Test behavior through the highest stable seam: in-process HTTP requests against the Minimal API.
- Add repository concurrency tests for exactly-one-winner semantics where HTTP setup would obscure the race.
- Cover authentication, RBAC, identity anti-impersonation, R-01, R-02, R-03, R-06, R-08, R-09, check-in, cancellation, and validation failures.
- Retain focused domain rule tests for deterministic policy calculations.
- Validate application compilation, all xUnit v3 tests, and Bicep compilation/linting.

## Out of Scope

Real SSO registration, real external APIs, real SQL persistence, physical badge/QR integration, payments, multi-site, multi-language, native applications, and production-scale networking are excluded.

## Further Notes

The repository ADRs that describe Azure SQL represent a broader architecture, but the active Spotly instructions mandate EF Core InMemory for this POC. This PRD follows the active constraint.

