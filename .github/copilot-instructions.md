# GitHub Copilot Instructions — Spotly

## Progetto
**Spotly** è una POC di Smart Office Booking (hackathon).
Stack: React 19 + Vite 8 + TypeScript 6 + Tailwind 4 (FE) | ASP.NET Core 10 LTS + C# 14 + SignalR (BE).
Tutte le integrazioni esterne sono **mockate** — non generare mai codice che chiami API esterne reali.

> Leggi `AGENTS.md` per le regole complete di dominio, stack e RBAC.

## Regole rapide per Copilot

### Frontend
- Usa **functional components** + hooks — mai `class extends Component`
- Imports: React non va importato esplicitamente (Vite lo gestisce), ma i tipi sì
- State locale: `useState` / `useReducer`; state globale: **Zustand 5**; server state: **TanStack Query v5**
- Ottimismo UI per booking: usa `useOptimistic` di React 19
- Styling: solo **Tailwind 4** — configurazione via `@theme` nel CSS, plugin `@tailwindcss/vite` in `vite.config.ts`; no `style={}` inline, no CSS modules
- Routing: `react-router` v7 (unico package, non più `react-router-dom`)
- Tutti gli endpoint API devono usare il **base URL da env** (`import.meta.env.VITE_API_URL`)
- In test e mock: usa **MSW v2** (`http.get(...)`, non `rest.get(...)`)

### Backend
- Pattern **Minimal API**: `app.MapGet(...)`, `app.MapPost(...)` — no `[ApiController]`
- Target framework: **`net10.0`** in tutti i `.csproj`
- DTOs: usa `record` con primary constructor — `public record CreateBookingRequest(Guid ResourceId, DateOnly Date);`
- Repositories: interfacce in `Spotly.Domain/`, implementazioni `InMemory*` in `Spotly.Infrastructure/`
- Validazione: **FluentValidation 12** — validazione **esplicita** negli endpoint (no auto-validation rimossa in v12)
- Testing: **xUnit v3** — usare `dotnet new xunit3` come template
- Logging: `ILogger<T>` + Serilog v3 — mai loggare email, nomi, posizioni GPS

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
