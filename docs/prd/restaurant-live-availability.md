# Disponibilità live e prenotazione ristorante

## Problem Statement

La demo Spotly deve mostrare un flusso ristorante concretamente eseguibile: i locali partecipanti provengono dal database SQL, inviano ogni dieci minuti un messaggio standard con il numero di posti disponibili e rispondono a ogni prenotazione con un codice di conferma o errore e il nuovo numero di posti residui. Per la demo odierna il canale è simulato come Telegram; i locali abilitati sono quelli che hanno un numero WhatsApp configurato nel database.

## Solution

Introdurre un protocollo partner versionato, un inbox idempotente, snapshot di disponibilità per locale/data, prenotazioni con lifecycle partner e un gateway Telegram mock. Un background service genera aggiornamenti ogni dieci minuti; endpoint Facility consentono di forzare messaggi e risultati per la demo. Ogni messaggio valido aggiorna SQL e produce un evento SignalR visualizzato dalla pagina pranzo.

## User Stories

1. As a Dipendente, I want to see only configured restaurants, so that every displayed venue can receive bookings.
2. As a Dipendente, I want to see current available seats per restaurant, so that I can make a realistic choice.
3. As a Dipendente, I want to see when availability was last updated, so that I can judge freshness.
4. As a Dipendente, I want to book one seat at a restaurant, so that my lunch is confirmed.
5. As a Dipendente, I want a coded confirmation with remaining seats, so that the outcome is unambiguous.
6. As a Dipendente, I want a coded error when the restaurant is full or unavailable, so that I can choose another venue.
7. As a Dipendente, I want restaurant counts to update immediately after booking, so that everyone sees the same inventory.
8. As a connected user, I want periodic partner updates to appear without refresh, so that the demo visibly receives messages from multiple venues.
9. As Facility, I want to force one availability update, so that the ten-minute flow can be demonstrated immediately.
10. As Facility, I want to simulate success, full, closed, and timeout outcomes, so that failure handling can be demonstrated.
11. As an operator, I want duplicate partner messages ignored, so that Telegram retries cannot apply availability twice.
12. As an operator, I want stale message sequences rejected, so that delayed updates cannot overwrite newer availability.
13. As an operator, I want malformed or unknown-restaurant messages rejected with a code, so that data remains trustworthy.
14. As an operator, I want partner payloads audited without employee identifiers or phone numbers in logs, so that diagnostics remain privacy-safe.
15. As a deployer, I want SQL Server selected through configuration and credentials supplied via environment, so that no secret enters source control.
16. As a developer, I want InMemory fallback for tests and local work, so that the suite remains deterministic.
17. As a future integrator, I want Telegram and WhatsApp behind one interface, so that the mock can be replaced without changing booking rules.
18. As a Dipendente, I want lunch-box fallback only when all restaurant availability is exhausted or service is outside hours, so that R-06 remains enforced.

## Implementation Decisions

- A restaurant is enabled when `IsActive` is true and `WhatsAppNumber` is configured in SQL. Raw phone numbers are never returned to clients or written to logs.
- Telegram chat identifiers are optional demo routing metadata; Telegram remains a mock gateway in the POC.
- Availability is restaurant-level, one seat per booking, keyed by restaurant and booking date.
- Standard availability message: `SPOTLY|1|AVAIL|messageId|restaurantId|date|available|sequence|sentAtUtc`.
- Standard booking request: `SPOTLY|1|BOOK|correlationId|restaurantId|date|partySize`.
- Standard partner response: `SPOTLY|1|BOOK_RESULT|correlationId|restaurantId|date|OK|remaining|partnerReference` or `...|ERROR_CODE|remaining|-`.
- Supported errors are `FULL`, `CLOSED`, `INVALID_DATE`, `NOT_CONFIGURED`, `TIMEOUT`, and `MALFORMED`.
- Inbox records message ID, kind, restaurant, sequence, received time, outcome, and raw payload. Raw partner payloads contain no employee identity.
- Availability updates accept only unseen message IDs and sequences greater than the current snapshot.
- A booking is created as `PendingPartner`; a successful response changes it to active/confirmed, while an error changes it to rejected and does not consume R-01 capacity.
- Partner remaining seats are authoritative after a response. Repository updates booking and availability inside one database critical section/transaction boundary.
- A configurable hosted service runs every ten minutes. Demo endpoints invoke the same ingestion/application services directly rather than duplicating logic.
- SignalR emits `RestaurantAvailabilityChanged` and `RestaurantMessageReceived` payloads without phone numbers or user IDs.
- SQL Server is enabled by `Database:Provider=SqlServer` and `ConnectionStrings:Spotly`; InMemory remains the default for local/test execution.

## Testing Decisions

- Primary seam: HTTP endpoint plus SignalR-facing application event behavior through in-process integration tests.
- Test parsing, malformed payload rejection, duplicate message idempotency, stale sequence rejection, multi-restaurant updates, successful booking, full/closed errors, R-01 and remaining-seat updates.
- Test the browser flow with Playwright: open Pranzo, observe venue counters, force a demo update, make a booking, and verify confirmation plus decremented availability.
- Tests assert externally visible state and codes, not repository implementation details.

## Out of Scope

Real Telegram Bot API calls, real WhatsApp Business API calls, webhook exposure to the public internet, payment, multi-seat group bookings, and production secret provisioning are excluded from this POC.

## Further Notes

The ten-minute interval is the configured operational cadence. Demo controls accelerate the same code path; they are Facility/Admin protected in the backend and simulated in MSW for the frontend-only demo.
