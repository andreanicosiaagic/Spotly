# ADR 002 — Backend Stack

**Data:** 20/06/2026  
**Stato:** Accepted  
**Contesto:** POC Spotly — hackathon, singola sede, deploy su Azure

---

## Decisione

Il backend di Spotly usa il seguente stack:

| Layer | Scelta | Alternativa scartata | Motivazione |
|---|---|---|---|
| Framework | **ASP.NET Core 8** | Node.js/Express, FastAPI | Competenze team, tipizzazione forte, SignalR built-in |
| API Style | **Minimal API** | Controller-based | Meno boilerplate, più leggibile, adatto a microservizi |
| Real-time | **SignalR** (built-in) | Socket.io, SSE | Integrato nel runtime, gestione hub tipizzata |
| Persistenza POC | **EF Core InMemory** | SQLite, PostgreSQL | Zero setup, seed semplice, sostituibile con EF reale |
| Logging | **Serilog** | NLog, built-in | Structured logging, sink Application Insights |
| Validazione | **FluentValidation** | DataAnnotations | Regole complesse (R-01..R-09), testabili isolatamente |
| Testing | **xUnit + Moq** | NUnit, FakeItEasy | Standard .NET, buona integrazione con WebApplicationFactory |

## Struttura soluzione

```
Spotly.sln
├── Spotly.Api/           ← Program.cs, endpoint groups, SignalR hubs
├── Spotly.Domain/        ← Entities, value objects, repository interfaces, domain rules
├── Spotly.Infrastructure/← InMemory repos, mock integrations (SSO, Graph, badge)
└── Spotly.Tests/         ← xUnit unit + integration tests
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

Questo permette di sostituire i mock con implementazioni reali senza toccare il domain.

## Conseguenze

- Nessun controller — solo `MapGroup` + extension methods
- Tutti i DTOs come `record` types C# 12
- Le business rules R-01..R-09 risiedono nel domain layer, non negli endpoint
