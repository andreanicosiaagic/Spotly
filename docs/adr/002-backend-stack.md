# ADR 002 — Backend Stack

**Data:** 20/06/2026  
**Stato:** Accepted  
**Contesto:** POC Spotly — hackathon, singola sede, deploy su Azure  
**Versioni verificate online:** giugno 2026

---

## Decisione

Il backend di Spotly usa il seguente stack:

| Layer | Scelta | Versione LTS/Stabile | Alternativa scartata | Motivazione |
|---|---|---|---|---|
| Runtime | **.NET** | **10.x** (LTS fino Nov 2028) | .NET 8, .NET 9 | LTS più recente; .NET 8 EOL Nov 2026, .NET 9 STS già scaduto |
| Framework | **ASP.NET Core** | **10.x** | Node.js/Express, FastAPI | Minimal API, SignalR built-in, tipizzazione forte |
| Linguaggio | **C#** | **14** (con .NET 10) | — | Primary constructors, record types, collection expressions |
| API Style | **Minimal API** | built-in .NET 10 | Controller-based | Meno boilerplate, più leggibile, adatto a POC |
| Real-time | **SignalR** | built-in .NET 10 | Socket.io, SSE | Integrato nel runtime, hub tipizzato |
| Persistenza POC | **EF Core InMemory** | **10.x** | SQLite, PostgreSQL | Zero setup, seed semplice, sostituibile con provider reale |
| Logging | **Serilog** | **v3.x** | NLog, built-in | Structured logging, sink Application Insights (.NET 10 ✅) |
| Validazione | **FluentValidation** | **12.x** (12.1.0) | DataAnnotations | Regole complesse (R-01..R-09), testabili isolatamente |
| Testing | **xUnit v3 + Moq** | xUnit **3.2.2** · Moq **v4.x** | NUnit, FakeItEasy | xUnit v3 è la versione corrente; v2 non più mantenuto |

## ⚠️ Note di compatibilità e breaking changes

### .NET 10 vs .NET 8 (upgrade raccomandato)
- **.NET 8** è ancora LTS ma va in EOL **novembre 2026** — sconsigliato per nuovi progetti
- **.NET 9** STS è già scaduto (May 2026)
- **.NET 10** è l'unico LTS attivo con supporto fino a novembre 2028
- Scaffold: `dotnet new webapi --framework net10.0`

### C# 14 (con .NET 10)
- Disponibile automaticamente con .NET 10 — nessun flag da attivare
- Feature rilevanti per Spotly: `field` keyword, `params` collections, `partial` properties

### FluentValidation 12.x (v11 → v12 — breaking)
- **Rimosso** il package `FluentValidation.AspNetCore` con auto-validation (era deprecato)
- Usare solo il package core + `FluentValidation.DependencyInjectionExtensions`
- Min target: .NET 8 (compatibile .NET 10 ✅)
```csharp
// Non più: services.AddFluentValidationAutoValidation()
// Ora: validazione esplicita negli endpoint
app.MapPost("/api/bookings", async (CreateBookingRequest req, IValidator<CreateBookingRequest> v) => {
    var result = await v.ValidateAsync(req);
    if (!result.IsValid) return Results.ValidationProblem(result.ToDictionary());
    // ...
});
```

### xUnit v3 (v2 → v3 — breaking)
- Template: `dotnet new xunit3` (non `xunit`)
- Architettura interna refactored — le extension di v2 possono non essere compatibili
- `Serilog.Sinks.XUnit` non supporta ancora ufficialmente v3 — usare console sink in test
- API per test async migliorata: `await Assert.ThrowsAsync<>()`

### EF Core 10 InMemory
- Richiede .NET 10 (non gira su .NET 8/9)
- Package: `Microsoft.EntityFrameworkCore.InMemory` versione 10.x
- Nuove feature (non necessarie per POC): vector search, native JSON columns

## Struttura soluzione

```
Spotly.sln
├── Spotly.Api/           ← Program.cs (net10.0), endpoint groups, SignalR hubs
├── Spotly.Domain/        ← Entities, value objects, repository interfaces, business rules
├── Spotly.Infrastructure/← InMemory repos, mock integrations (SSO, Graph, badge)
└── Spotly.Tests/         ← xUnit v3 unit + integration tests
```

## Pattern Repository

Tutte le integrazioni esterne vivono dietro interfacce in `Domain/`:

```csharp
// Domain — interfaccia
public interface IBookingRepository { ... }
public interface ICalendarIntegration { ... }  // mock → Graph reale

// Infrastructure — implementazione mock
public class InMemoryBookingRepository : IBookingRepository { ... }
public class MockCalendarIntegration : ICalendarIntegration { ... }
```

## Conseguenze

- Target framework: `net10.0` in tutti i `.csproj`
- Nessun controller — solo `MapGroup` + extension methods
- Tutti i DTOs come `record` types C# 14
- FluentValidation: validazione esplicita negli endpoint (no auto-validation)
- xUnit: usare template `xunit3`, non `xunit`
- Le business rules R-01..R-09 risiedono nel domain layer, non negli endpoint
