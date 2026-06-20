# GitHub Copilot Instructions — Spotly

## Progetto
**Spotly** è una POC di Smart Office Booking (hackathon).
Stack: React 18 + Vite + TypeScript + Tailwind (FE) | ASP.NET Core 8 Minimal API + SignalR (BE).
Tutte le integrazioni esterne sono **mockate** — non generare mai codice che chiami API esterne reali.

> Leggi `AGENTS.md` per le regole complete di dominio, stack e RBAC.

## Regole rapide per Copilot

### Frontend
- Usa **functional components** + hooks — mai `class extends Component`
- Imports: React non va importato esplicitamente (Vite lo gestisce), ma i tipi sì
- State locale: `useState` / `useReducer`; state globale: **Zustand**; server state: **TanStack Query**
- Styling: solo **Tailwind** — no `style={}` inline, no CSS modules, no styled-components
- Tutti gli endpoint API devono usare il **base URL da env** (`import.meta.env.VITE_API_URL`)
- In test e mock: usa **MSW v2** (`http.get(...)`, non `rest.get(...)`)

### Backend
- Pattern **Minimal API**: `app.MapGet(...)`, `app.MapPost(...)` — no `[ApiController]`
- DTOs: usa `record` con primary constructor — `public record CreateBookingRequest(Guid ResourceId, DateOnly Date);`
- Repositories: interfacce in `Spotly.Domain/`, implementazioni `InMemory*` in `Spotly.Infrastructure/`
- Validazione: **FluentValidation** per input degli endpoint
- Logging: `ILogger<T>` + Serilog — mai loggare email, nomi, posizioni GPS

### Entrambi
- Nessun segreto hardcodato — usa `appsettings.Development.json` (BE) o `.env.local` (FE)
- Ogni nuova feature deve avere almeno un test unitario

## Struttura cartelle attesa

```
frontend/src/
  modules/parking/     ← M1
  modules/desk/        ← M2
  modules/lunch/       ← M3
  components/          ← UI condivisi
  hooks/               ← hook condivisi
  mocks/               ← MSW handlers
  store/               ← Zustand stores
  pages/               ← route-level pages

backend/
  Spotly.Api/          ← endpoints, hubs, program.cs
  Spotly.Domain/       ← entities, interfaces, business rules
  Spotly.Infrastructure/ ← InMemory repos, mock integrations
  Spotly.Tests/        ← xUnit tests
```

## Easter egg
Il componente `<CatRain />` è **intenzionale** e **non deve essere rimosso**. Vedi skill `spotly-cats`.
