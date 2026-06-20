# ADR 001 — Frontend Stack

**Data:** 20/06/2026  
**Stato:** Accepted  
**Contesto:** POC Spotly — hackathon, singola sede, mobile-first PWA

---

## Decisione

Il frontend di Spotly usa il seguente stack:

| Layer | Scelta | Alternativa scartata | Motivazione |
|---|---|---|---|
| Bundler | **Vite** | CRA, Next.js | HMR veloce, zero config, PWA plugin disponibile |
| UI Framework | **React 18** | Vue, Svelte | Competenze team, ecosystem, Concurrent features |
| Linguaggio | **TypeScript 5** | JavaScript | Type safety, IntelliSense, contratti API espliciti |
| Styling | **Tailwind CSS 3** | MUI, styled-components | Mobile-first utility-first, nessun runtime overhead |
| Routing | **React Router v6** | TanStack Router | Maturo, documentato, SPA standard |
| State globale | **Zustand** | Redux Toolkit, Jotai | Minimal boilerplate, ergonomia, bundle size |
| Server state | **TanStack Query v5** | SWR, Apollo | Cache management, background refetch, optimistic updates |
| Real-time | **@microsoft/signalr** | Socket.io, WebSocket raw | Allineamento con SignalR backend, reconnect automatico |
| Mock API | **MSW v2** | json-server, axios-mock-adapter | Intercetta fetch nativo, funziona in browser e test |
| Testing | **Vitest + Testing Library** | Jest | Integrato con Vite, stessa config |
| E2E | **Playwright** | Cypress | Cross-browser, skill `webapp-testing` |

## Conseguenze

- Tutti i componenti devono essere functional — nessun class component
- Il design system usa token Tailwind (vedi ADR 001-b theme-factory)
- MSW gestisce TUTTI i mock API — nessuna fetch reale in dev/test
