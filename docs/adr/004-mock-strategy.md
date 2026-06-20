# ADR 004 — Strategia Mock per la POC

**Data:** 20/06/2026  
**Stato:** Accepted  
**Contesto:** La POC moca tutte le integrazioni esterne (SSO, Graph, badge, welfare, partner ristorazione)

---

## Principio

> **"Mock dietro interfacce"** — ogni integrazione esterna esiste come interfaccia nel domain layer. La POC usa implementazioni mock. Il prodotto sostituirà i mock con connettori reali senza modificare il domain.

## Mappa delle integrazioni

| Integrazione | Interfaccia (Domain) | Mock (Infrastructure) | Connettore reale (futuro) |
|---|---|---|---|
| Autenticazione SSO | `IIdentityProvider` | `MockIdentityProvider` (JWT stub, utenti hardcoded) | MSAL + Entra ID |
| Calendario Outlook | `ICalendarIntegration` | `MockCalendarIntegration` (log only) | Microsoft Graph |
| Badge / tornelli | `IAccessControlSystem` | `MockAccessControlSystem` (sempre OK) | Connettore badge vendor |
| Partner ristorazione | `IRestaurantPartner` | `MockRestaurantPartner` (menu/capienza statici) | API REST partner |
| Welfare / buoni pasto | `IWelfareProvider` | `MockWelfareProvider` (budget illimitato) | SDK Edenred/Pellegrini |
| Notifiche push/email | `INotificationService` | `MockNotificationService` (log to console) | Azure Communication Services |

## Frontend: MSW v2

Il FE usa **Mock Service Worker** per intercettare le chiamate HTTP:

```
src/mocks/
  handlers/
    parking.handlers.ts    ← GET /api/parking/availability, POST /api/bookings
    desk.handlers.ts
    lunch.handlers.ts
    auth.handlers.ts       ← POST /api/auth/token (mock JWT)
  browser.ts               ← MSW browser setup
  server.ts                ← MSW Node setup per Vitest
```

I mock restituiscono dati seed realistici definiti in `src/mocks/data/`.

## Utenti seed (POC)

```typescript
// Utenti hardcoded per il mock JWT
const SEED_USERS = [
  { id: "u1", name: "Giulia Romano", role: "Dipendente", email: "giulia@spotly.test" },
  { id: "u2", name: "Marco Bianchi", role: "Manager",    email: "marco@spotly.test" },
  { id: "u3", name: "Sara Conti",    role: "Facility",   email: "sara@spotly.test" },
  { id: "u4", name: "Admin",         role: "Admin",      email: "admin@spotly.test" },
];
```

## Conseguenze

- **Nessun side effect reale** in dev/test: nessuna email inviata, nessun calendario toccato
- I test unitari iniettano i mock via DI — `InMemory*` repositories nel container test
- Il passaggio al prodotto richiede solo di registrare le implementazioni reali nel DI container
- La skill `entra-app-registration` guida la transizione SSO mock → Entra ID reale
