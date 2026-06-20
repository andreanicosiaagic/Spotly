# Spotly — Smart Office Booking

> **POC** — Hackathon "App-in-a-Day" · Singola sede · Integrazioni mockate

Spotly consente ai dipendenti di organizzare la propria giornata in ufficio da un'unica interfaccia **mobile-first**: prenotazione di **parcheggio**, **postazione di lavoro** e **pranzo** con visibilità in tempo reale sulla disponibilità.

---

## Stack

| Layer | Tecnologie |
|---|---|
| Frontend | React 18 · Vite · TypeScript · Tailwind CSS · Zustand · TanStack Query · SignalR client |
| Backend | ASP.NET Core 8 Minimal API · SignalR · EF Core InMemory · Serilog |
| Real-time | SignalR hub (`AvailabilityHub`) + lock ottimistico |
| Mock | MSW v2 (FE) · InMemory repositories (BE) |
| Deploy | Azure App Service |

## Moduli POC

- **M1 — Parcheggio:** mappa SVG interattiva, prenotazione/cancellazione, disponibilità real-time
- **M2 — Postazioni/Desk:** mappa per zona/piano, hot-desking, abbinamento auto↔scrivania suggerito
- **M3 — Pranzo:** locali convenzionati con slot, fallback lunch box quando sono al completo
- **M0 — Trasversale:** dashboard "La mia giornata", auth mock (RBAC: Dipendente/Manager/Facility/Admin)

## Struttura

```
.agents/
  skills/          ← skill installate (harness)
  hooks/           ← pre-tool-call, post-file-edit
.github/
  copilot-instructions.md
docs/
  adr/             ← Architecture Decision Records (001–004)
spotly-cats/       ← sorgente skill easter egg 🐱
AGENTS.md          ← regole dominio, stack, RBAC, proibizioni
01-analisi-funzionale.md
```

## Easter egg 🐱

Konami code `↑↑↓↓←→←→BA` oppure 5× click sul logo → pioggia di gattini.

## Avvio rapido

```bash
# Frontend
cd frontend && npm install && npm run dev

# Backend
cd backend && dotnet run --project Spotly.Api
```

> Il progetto è una **POC**. Nessuna integrazione esterna reale: SSO, Outlook, badge e welfare sono tutti mockati.
