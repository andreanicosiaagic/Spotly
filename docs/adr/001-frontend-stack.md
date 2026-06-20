# ADR 001 — Frontend Stack

**Data:** 20/06/2026  
**Stato:** Accepted  
**Contesto:** POC Spotly — hackathon, singola sede, mobile-first PWA  
**Versioni verificate online:** giugno 2026

---

## Decisione

Il frontend di Spotly usa il seguente stack:

| Layer | Scelta | Versione LTS/Stabile | Alternativa scartata | Motivazione |
|---|---|---|---|---|
| Bundler | **Vite** | **8.x** (8.0.16) | CRA, Next.js | Rolldown (Rust bundler), HMR veloce, PWA plugin |
| UI Framework | **React** | **19.x** (19.2.7) | Vue, Svelte | Concurrent features, Server Components, nuovi hook |
| Linguaggio | **TypeScript** | **6.x** (6.0.3) | JavaScript | Type safety, 7.0 RC in arrivo ma non ancora stabile |
| Styling | **Tailwind CSS** | **4.x** (4.3.1) | MUI, styled-components | CSS-first config, Oxide engine (10× più veloce), no runtime overhead |
| Routing | **React Router** | **v7** (7.17.0) | TanStack Router | File-based routing, SSR/Remix features, integrazione React 19 |
| State globale | **Zustand** | **5.x** (5.0.0) | Redux Toolkit, Jotai | Minimal boilerplate, compatibile React 19 |
| Server state | **TanStack Query** | **v5** (5.101.0) | SWR, Apollo | Cache management, background refetch, optimistic updates |
| Real-time | **@microsoft/signalr** | bundled .NET 10 | Socket.io, WebSocket raw | Allineamento con SignalR backend, reconnect automatico |
| Mock API | **MSW** | **v2** (2.14.6) | json-server, axios-mock-adapter | Intercetta fetch nativo, WebSocket support (v2.14+) |
| Testing | **Vitest** | **4.x** (4.1.8) | Jest | Integrato con Vite 8, stessa config; v5 in beta |
| E2E | **Playwright** | **1.61.0** | Cypress | Cross-browser, skill `webapp-testing` |

## ⚠️ Note di compatibilità e breaking changes

### Tailwind CSS v4 (v3 → v4 — breaking)
- La configurazione è ora **CSS-first** via direttiva `@theme` nel CSS principale; `tailwind.config.js` è opzionale ma deprecato
- Si usa il **plugin Vite nativo** `@tailwindcss/vite` invece di PostCSS
- Tutte le classi utility di v3 sono ancora disponibili (migration automatica disponibile)
- I CSS custom properties (`--color-*`, `--spacing-*`) sono esposti nativamente per il runtime theming
```css
/* Invece di tailwind.config.js, si usa: */
@import "tailwindcss";
@theme {
  --color-primary: #6366f1;
}
```

### React 19 (v18 → v19 — cambio consigliato, non breaking per POC)
- Nuovi hook utili per Spotly: `useOptimistic` (stato ottimistico booking), `useActionState` (form prenotazione)
- React 18 rimane supportato ma non riceve nuove feature
- `react-dom/client` API invariata per SPA

### React Router v7 (v6 → v7 — breaking)
- Package rinominato: `react-router-dom` → `react-router` (unico package)
- Supporto opzionale per file-based routing e SSR (non necessario per POC)
- API `<BrowserRouter>`, `useNavigate`, `useParams` invariate per SPA classica

### Zustand v5 (v4 → v5)
- API di base invariata; breaking changes riguardano middleware avanzati
- Richiede React 18+ (compatibile con React 19 ✅)

### Vite 8 (cambio interno bundler)
- Il bundler sottostante è ora **Rolldown** (Rust): build produzione più veloce
- `vite.config.ts` API invariata; plugin ecosystem aggiornato

## Conseguenze

- Tutti i componenti devono essere functional — nessun class component
- Tailwind v4: configurazione via `@theme` nel CSS, plugin `@tailwindcss/vite` in `vite.config.ts`
- MSW gestisce TUTTI i mock API — nessuna fetch reale in dev/test
- Usare `useOptimistic` di React 19 per il feedback immediato sul lock ottimistico (R-03)
