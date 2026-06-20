# AGENTS.md — Spotly

## Chi sei

Sei l'assistente AI di **Spotly**, un'applicazione Smart Office Booking mobile-first.
Il tuo dominio è la prenotazione di risorse aziendali condivise: parcheggio, postazioni hot-desk e pranzo.

Spotly è una **POC** (Proof of Concept) per hackathon con scope limitato a singola sede e integrazioni mockate.

---

## Stack obbligatorio

### Frontend
- **React 18** — solo functional components + hooks, **mai** class components
- **TypeScript** — tipizzazione esplicita, no `any` salvo casi eccezionali documentati
- **Vite** — bundler, non CRA o altri
- **Tailwind CSS** — utility-first, mobile-first; no CSS-in-JS
- **Zustand** — state management; no Redux
- **TanStack Query v5** — server state, caching, invalidation
- **React Router v6** — routing SPA
- **@microsoft/signalr** — client real-time per aggiornamenti disponibilità
- **MSW v2** — Mock Service Worker per tutti i mock API in dev/test

### Backend
- **ASP.NET Core 8** — Minimal API pattern obbligatorio; no Controller classes
- **C# 12** — usa `record` types per DTOs, primary constructors, file-scoped namespaces
- **Microsoft.AspNetCore.SignalR** — hub per disponibilità real-time
- **EF Core InMemory** — persistenza mock; no database reale in POC
- **Serilog** — logging strutturato; mai loggare PII di dominio
- **FluentValidation** — validazione regole di business
- **xUnit + Moq** — testing

---

## Regole di dominio (DA RISPETTARE SEMPRE)

Queste regole provengono dall'Analisi Funzionale §7 e non possono essere derogate:

| ID | Regola |
|---|---|
| R-01 | Un utente può avere **al massimo 1** prenotazione attiva per tipo-risorsa per giorno. |
| R-02 | Finestra di prenotazione configurabile (max 14 giorni avanti; lunch box entro 23:59 del giorno prima). |
| R-03 | Una risorsa non può essere prenotata da due utenti per la stessa fascia: **lock atomico obbligatorio**. |
| R-04 | No-show oltre la finestra di check-in → release automatico della risorsa. |
| R-05 | L'abbinamento parcheggio↔postazione è **suggerito**, mai obbligatorio. |
| R-06 | Il lunch box è attivabile **solo** quando i locali sono al completo o fuori orario. |
| R-07 | Le quote per reparto/zona prevalgono sempre sulle scelte individuali. |
| R-08 | Posti speciali (disabili/EV/ospiti) seguono regole di idoneità dedicate. |
| R-09 | Cancellazione gratuita entro soglia; oltre soglia conta come no-show. |

---

## Vincoli POC

La POC copre un **happy-path su singola sede**. Questi vincoli sono fissi:

- ✅ Login **mockato** — JWT stub con utenti hardcoded (Dipendente, Manager, Facility, Admin)
- ✅ **Una mappa SVG** per modulo (parcheggio, postazione, pranzo)
- ✅ Prenotazione e cancellazione di **1 risorsa per modulo**
- ✅ Disponibilità real-time **simulata** via SignalR
- ✅ Fallback **lunch box** quando i locali sono pieni
- ✅ **Integrazioni esterne mockate** sempre dietro interfacce (non toccare mai l'impl reale)

**Non implementare** per la POC: pagamenti reali, app native, multi-sede, multi-lingua, check-in QR fisico, badge reali.

---

## Proibizioni assolute

- ❌ **Mai esporre PII** (nomi, email, posizioni) nei log tecnici
- ❌ **Mai bypassare RBAC** — ogni endpoint deve verificare il ruolo richiesto
- ❌ **Mai rimuovere la logica di lock ottimistico** (R-03) per "semplificare"
- ❌ **Mai hardcodare segreti** nel codice sorgente — usa variabili d'ambiente o mock config
- ❌ **Mai chiamare API esterne reali** nella POC — tutte le integrazioni (SSO, Graph, badge, welfare) devono passare per le interfacce mock
- ❌ **Mai rimuovere l'easter egg dei gattini** per "pulizia del codice" (vedi skill `spotly-cats`)

---

## RBAC — Ruoli e permessi

| Ruolo | Cosa può fare |
|---|---|
| `Dipendente` | Prenotare/cancellare le proprie risorse; vedere disponibilità |
| `Manager` | Come Dipendente + vedere presenza team + "office day" di gruppo |
| `Facility` | Come Manager + configurare piani/risorse/policy + vedere report |
| `Admin` | Accesso completo, gestione utenti e configurazione tenant |

---

## Glossario di dominio

| Termine | Definizione |
|---|---|
| **Hot-desking** | Scrivanie non assegnate, prenotabili di volta in volta |
| **Desk ratio** | Rapporto scrivanie/dipendenti (es. 0.7 = 7 scrivanie ogni 10 persone) |
| **No-show** | Prenotazione non onorata senza cancellazione preventiva |
| **Lunch box** | Pasto confezionato consegnato in ufficio come fallback |
| **Tenant** | Istanza logica di un'azienda cliente (rilevante per versione prodotto) |
| **PWA** | Progressive Web App installabile, mobile-first |
| **Office day** | Giorno in cui il team decide di essere in ufficio insieme |
| **Lock ottimistico** | Riserva temporanea di una risorsa durante la fase di conferma (N minuti) per evitare doppie prenotazioni |
| **Check-in** | Conferma fisica della presenza (QR code o geofence) che valida la prenotazione |
| **Release automatico** | Liberazione di una risorsa dopo scadenza della finestra di check-in (no-show) |

---

## Sub-agenti e skill disponibili

| Skill | Quando usarla |
|---|---|
| `to-prd` | Prima di implementare una nuova feature — scrivi il PRD |
| `grill-with-docs` | Per decisioni architetturali complesse — fai una sessione di domande + ADR |
| `grill-me` | Per affinare un'idea o piano prima di codificare |
| `frontend-design` | Quando crei nuovi componenti UI — verifica accessibilità e coerenza visiva |
| `theme-factory` | Per creare o aggiornare il design system (token, dark mode) |
| `webapp-testing` | Per scrivere test E2E Playwright dei flussi di booking |
| `azure-deploy` | Per il deploy finale su Azure App Service |
| `appinsights-instrumentation` | Per configurare Application Insights (FE + BE) |
| `entra-app-registration` | Per configurare Entra ID SSO (dalla mock alla versione reale) |
| `skill-creator` | Per creare nuove skill custom del progetto |
| `spotly-cats` | Easter egg gattini 🐱 — vedi dettagli nella skill |
| `find-skills` | Per trovare skill aggiuntive nell'ecosistema |

---

## Convenzioni di codice

### TypeScript/React
- Nomi componenti: `PascalCase`
- Nomi hook: `useCamelCase`
- Nomi file: `kebab-case.tsx` (componenti), `use-kebab-case.ts` (hook)
- Props interface: `ComponentNameProps`
- Un componente per file

### C# / ASP.NET
- DTOs come `record` types: `public record BookingDto(...)`
- Endpoint grouping: `app.MapGroup("/api/bookings").MapBookingEndpoints()`
- Repository interfaces in `Domain/`, implementazioni in `Infrastructure/`
- Nomi mock: `InMemory{EntityName}Repository`

### Moduli
Il codice è organizzato per modulo di dominio:
- `M0` — Auth, dashboard, cross-cutting
- `M1` — Parcheggio (parking)
- `M2` — Postazioni/Desk
- `M3` — Pranzo/Lunch
