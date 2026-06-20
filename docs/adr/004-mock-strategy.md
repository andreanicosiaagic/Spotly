# ADR 004 — Strategia Mock per la POC

**Data:** 20/06/2026  
**Stato:** Accepted *(aggiornato: autenticazione → Entra ID Easy Auth; no MockIdentityProvider)*  
**Contesto:** La POC usa Entra ID Easy Auth per autenticazione; tutte le altre integrazioni esterne rimangono mockate

---

## Principio

> **"Entra ID per authn, mock per tutto il resto"** — l'autenticazione usa Entra ID Easy Auth (nessuna riga di codice). Le integrazioni esterne di dominio (calendario, badge, welfare, partner) rimangono dietro interfacce mock. Il dominio non sa nulla di Entra ID.

## Autenticazione: Entra ID Easy Auth (non mockato)

L'autenticazione è gestita da **Azure App Service Easy Auth** con provider Entra ID:
- Nessuna riga di codice auth nell'applicazione
- Easy Auth intercetta ogni request non autenticata → redirect OIDC a `login.microsoftonline.com`
- Il token JWT (claims: `oid`, `preferred_username`, `roles`) è disponibile come header `X-MS-CLIENT-PRINCIPAL`
- Il backend legge i claims dal token per RBAC — **nessun `IIdentityProvider` mock**

```csharp
// Lettura claims dall'header Easy Auth (Minimal API)
app.MapGet("/api/me", (HttpContext ctx) =>
{
    var principal = ctx.User; // popolato da Easy Auth middleware
    var userId = principal.FindFirst("oid")?.Value;
    var roles  = principal.FindAll("roles").Select(c => c.Value);
    return Results.Ok(new { userId, roles });
}).RequireAuthorization();
```

> **RBAC:** Easy Auth gestisce autenticazione (chi sei). Per autorizzazione (cosa puoi fare) occorre definire **App Roles** nel manifest dell'App Registration Entra e assegnarli agli utenti. I claims `roles` saranno presenti nel token JWT automaticamente.

---

## Mappa delle integrazioni (solo quelle mockate)

| Integrazione | Interfaccia (Domain) | Mock (Infrastructure) | Connettore reale (futuro) |
|---|---|---|---|
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
  browser.ts               ← MSW browser setup
  server.ts                ← MSW Node setup per Vitest
```

> ⚠️ **Nessun `auth.handlers.ts`** — Easy Auth è infrastruttura Azure, non viene mockato da MSW.
> In sviluppo locale, usare `Static Web Apps CLI` o un proxy che simula gli header Easy Auth (`X-MS-CLIENT-PRINCIPAL`).

I mock restituiscono dati seed realistici definiti in `src/mocks/data/`.

## Utenti seed (dev locale — header Easy Auth simulati)

```typescript
// src/mocks/data/users.ts — usato per simulare X-MS-CLIENT-PRINCIPAL in locale
export const SEED_USERS = [
  { oid: "u1", name: "Giulia Romano", roles: ["Dipendente"], email: "giulia@spotly.test" },
  { oid: "u2", name: "Marco Bianchi", roles: ["Manager"],    email: "marco@spotly.test" },
  { oid: "u3", name: "Sara Conti",    roles: ["Facility"],   email: "sara@spotly.test" },
  { oid: "u4", name: "Admin",         roles: ["Admin"],      email: "admin@spotly.test" },
];
// I ruoli sono App Roles definiti nel manifest dell'App Registration Entra
```

## Conseguenze

- **Nessun `IIdentityProvider` mock** — authn delegata a Easy Auth
- **Nessun side effect reale** in dev/test: nessuna email inviata, nessun calendario toccato
- I test unitari iniettano i mock via DI — Azure SQL con `UseInMemoryDatabase` solo nei test di Infrastructure
- Il passaggio al prodotto richiede solo di registrare le implementazioni reali nel DI container
- La skill `entra-app-registration` guida la configurazione dell'App Registration con App Roles
