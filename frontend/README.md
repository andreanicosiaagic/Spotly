# Spotly Frontend

Frontend mobile-first della POC Spotly.

Stack operativo:

- React 19 + TypeScript 6
- Vite 8
- Tailwind CSS 4
- Zustand 5 per stato UI
- TanStack Query 5 per stato server
- SignalR per aggiornamenti realtime
- MSW 2 solo in modalità mock esplicita

## Porte locali

- frontend Vite: `http://127.0.0.1:5173`
- backend ASP.NET: `http://localhost:5205`
- harness E2E isolato: frontend `55173`, backend `55205`

Il proxy Vite inoltra:

- `/api/*` → backend
- `/availability` → hub SignalR backend

## Modalità supportate

### 1. Modalità integrata reale

È la modalità raccomandata per la demo locale: frontend vero + backend vero + SignalR vero.

Non serve attivare MSW.

```bash
npm install
npm run dev -- --host 127.0.0.1 --port 5173
```

In un secondo terminale:

```bash
cd ../backend
dotnet run --launch-profile http --project Spotly.Api
```

### 2. Modalità mock esplicita

Usare solo quando serve isolare il frontend dal backend.

Creare un `.env.local` partendo da `.env.example` e impostare:

```bash
VITE_USE_MSW=true
```

In questa modalità MSW intercetta le API del browser. Non è la configurazione di default della demo.

### 3. Modalità direct API (solo se serve)

Se vuoi bypassare il proxy Vite e puntare direttamente a un backend remoto o dedicato, imposta:

```bash
VITE_USE_DIRECT_API=true
VITE_API_URL=http://localhost:5205
```

In sviluppo locale standard questa modalità non è necessaria ed espone a problemi CORS se il backend non è configurato di conseguenza.

## Autenticazione demo

La UI permette di cambiare profilo demo direttamente dalla sidebar/header.

Profili disponibili:

- `Dipendente`
- `Manager`
- `Facility`
- `Admin`

Il frontend ottiene l’identità reale della sessione demo da `GET /api/me` e usa un bearer token dev per:

- chiamate HTTP browser;
- negoziazione SignalR;
- verifica RBAC reale sul backend.

## Scenario demo ristorante

Per la demo pranzo:

- i locali vengono letti dal backend mockato su repository EF InMemory;
- la conferma/errore partner è codificata;
- i posti disponibili vengono aggiornati sia da feed periodico sia da prenotazioni/cancellazioni;
- il fallback lunch box è esposto con endpoint di eleggibilità dedicato;
- il giorno demo lunch box è `oggi + 1`, con locali seedati a capacità zero per rispettare R-06.

## E2E integrato

Installazione dipendenze test:

```bash
npm run e2e:install
```

Smoke test locale integrato:

```bash
npm run e2e:integrated
```

Il test esegue:

- verifica RBAC (`Manager` → `403`, `Facility` → `200`) sul tick demo ristorante;
- verifica push realtime SignalR senza MSW;
- prenotazione ristorante con slot e menu;
- persistenza server-backed dopo refresh;
- riflesso booking in dashboard;
- cancellazione della prenotazione.

Lo script E2E usa porte dedicate (`55173` / `55205`) per non dipendere da eventuali processi demo lasciati attivi sulle porte standard.

## Easter egg gattini

L’easter egg è attivo con:

- Konami code;
- 5 click rapidi sul logo Spotly.

Per evitare dipendenze obbligatorie dalla rete in demo, il rain usa asset locale di default.
Se vuoi abilitare le immagini remote di `cataas.com`, imposta:

```bash
VITE_CAT_RAIN_REMOTE=true
```

## Troubleshooting

- Se il realtime resta offline, verificare che il backend sia attivo sulla porta `5205`.
- Se vedi dati finti in development, controllare che `VITE_USE_MSW` non sia `true`.
- Se cambi profilo demo e i permessi non si aggiornano, ricaricare la pagina per azzerare eventuale stato locale residuo.
