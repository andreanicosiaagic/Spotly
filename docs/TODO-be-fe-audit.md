# TODO audit backend e frontend

Data audit: 20 giugno 2026

## Perimetro

Audit di backend, frontend, contratti HTTP/SignalR, regole di dominio, RBAC, persistenza, integrazioni mock e test.

Esclusi intenzionalmente:

- IaC e configurazione di deploy, gestiti da un altro sviluppatore;
- rendering, componenti e dati delle mappe parcheggio/postazioni, modificati in parallelo da un altro agente.

Le modifiche concorrenti presenti durante l'audit non sono state toccate.

## Risultato sintetico

- Build backend e frontend: superate.
- Test backend: 25/25 superati.
- E2E mock ristorante e disponibilità team: superati.
- Vulnerabilità note: nessuna rilevata da `npm audit --omit=dev` e `dotnet list package --vulnerable --include-transitive`.
- Copertura backend: 58,19% linee, 26,53% branch; API 49,33% linee, 14,31% branch.
- L'avvio frontend standard non esercita il backend: MSW intercetta le API e il proxy Vite punta alla porta 5000, mentre il backend usa la 5205. SignalR risponde `502` e l'errore viene nascosto.
- Un build di produzione configurato esplicitamente verso un backend isolato ha ricevuto correttamente un aggiornamento ristorante via SignalR/long polling. Il nucleo funziona, ma manca un percorso locale/integrato ripetibile.

## P0 — blocca demo affidabile o regole inderogabili

### TODO-001 — Collegare realmente frontend e backend nella modalità demo

**Evidenza:** `frontend/src/main.tsx:6-10` abilita sempre MSW in development; `frontend/vite.config.ts:9-16` inoltra API e SignalR a `localhost:5000`, ma `backend/Spotly.Api/Properties/launchSettings.json:8` espone `localhost:5205`. Nel browser l'audit ha osservato API `200` generate da MSW e negoziazione SignalR `502`.

**Impatto:** la demo può sembrare funzionante senza esercitare RBAC, repository, protocollo ristoranti, regole server o push real-time reali.

**Completamento:**

- introdurre una variabile esplicita, per esempio `VITE_USE_MSW`, invece di legare MSW a ogni build dev;
- allineare proxy e porta backend;
- definire una strategia di autenticazione dev che funzioni anche per SignalR;
- aggiungere uno smoke E2E FE↔BE senza MSW che verifica prenotazione e aggiornamento push senza polling.

### TODO-002 — Rendere le prenotazioni server-backed e indicizzate per data

**Evidenza:** `frontend/src/store/bookingStore.ts:6-23` conserva una sola prenotazione per tipo, non per data, e solo in memoria. `frontend/src/pages/DashboardPage.tsx:13` visualizza quello stato per qualunque data selezionata. Un refresh azzera tutto. Esiste `GET /api/parking/bookings/me`, ma mancano gli equivalenti desk e lunch.

**Impatto:** la dashboard mostra prenotazioni della data sbagliata e perde lo stato al refresh; il server può contenere una prenotazione che il client non mostra.

**Completamento:**

- aggiungere endpoint uniformi `GET /bookings/me?date=...` per tutti i moduli;
- usare TanStack Query come fonte server e Zustand solo per stato UI;
- includere la data nelle query key e invalidare dopo create/delete/release;
- verificare refresh, cambio data e apertura in una seconda scheda.

### TODO-003 — Completare la prenotazione ristorante con slot e scelta pasto

**Evidenza:** `frontend/src/pages/LunchPage.tsx:26-29` invia solo `restaurantId`, `isLunchBox` e data. Non legge `/slots` né `/menu`. `CreateLunchBookingValidator` richiede solo il ristorante e `BeginRestaurantBookingAsync` non valida lo slot. L'audit ha ottenuto `201 Created` con `slotId: null`.

**Impatto:** M3-01/M3-02 sono solo parzialmente implementati; il locale riceve una prenotazione senza fascia oraria o pasto.

**Completamento:**

- aggiungere selezione obbligatoria di slot e almeno riepilogo del menu;
- richiedere e validare `SlotId` rispetto a ristorante/data/capienza;
- includere lo slot nel comando partner codificato;
- aggiornare atomicamente disponibilità globale e disponibilità dello slot;
- coprire slot pieno, slot non appartenente al locale e prenotazione concorrente.

### TODO-004 — Rendere dimostrabile il fallback lunch box senza violare R-02/R-06

**Evidenza:** la UI mostra sempre la scheda Lunch Box (`LunchPage.tsx:38-61`), ma R-02 vieta il lunch box per il giorno corrente (`BookingRules.cs:45-52`). La data iniziale è oggi. Un tentativo runtime per oggi ha restituito `422`. L'idoneità R-06 viene scoperta solo dopo il click.

**Impatto:** il fallback dichiarato come requisito POC non è dimostrabile nel flusso predefinito e la UI propone azioni che il server rifiuta.

**Completamento:**

- esporre dal backend un'indicazione di eleggibilità con motivazione;
- disabilitare/nascondere il catalogo quando R-02 o R-06 non sono soddisfatte;
- preparare uno scenario demo per domani con locali pieni, senza introdurre bypass alle regole;
- mostrare chiaramente cutoff e motivazione del fallback.

### TODO-005 — Popolare e aggiornare il pranzo per tutta la finestra prenotabile

**Evidenza:** `SpotlyDbSeeder` crea disponibilità, slot e menu solo per oggi (`SpotlyDbContext.cs:46-50`). Per domani l'audit ha ricevuto locali con zero posti, sequenza zero, timestamp anno 1 e nessuno slot. Il poller aggiorna solo la data corrente (`RestaurantAvailabilityPollingService.cs:17-18`).

**Impatto:** R-02 consente 14 giorni, ma il modulo pranzo è utilizzabile solo oggi; domani è l'unica data valida per il lunch box ma non consente una normale prenotazione ristorante.

**Completamento:** seed deterministico/on-demand per 14 giorni, slot/menu coerenti e aggiornamenti demo sulle date visualizzate.

### TODO-006 — Garantire R-03 e R-01 anche con SQL e più istanze

**Evidenza:** i repository serializzano con un `Lock` in memoria del processo (`InMemoryParkingRepository.cs:11`, `InMemoryDeskRepository.cs:11`, `InMemoryLunchRepository.cs:12`). Il modello SQL non contiene vincoli univoci per risorsa/data o utente/data (`SpotlyDbContext.cs:23-35`). L'endpoint lock non riceve la data e i repository bloccano sempre oggi (`InMemoryParkingRepository.cs:120-129`, equivalente desk). L'audit ha verificato che un lock richiesto mentre si opera su domani rende `pending` oggi e lascia domani `available`.

**Impatto:** il test concorrente passa solo in una singola istanza/repository; con SQL o scale-out sono possibili doppie prenotazioni. Il lock ottimistico non protegge la data scelta.

**Completamento:**

- aggiungere la data al contratto di lock;
- introdurre transazione atomica e vincoli/indici univoci filtrati appropriati;
- usare concurrency token o tabella lock con scadenza;
- aggiungere test concorrenti con repository distinti sullo stesso database relazionale;
- mantenere R-01 e R-03 anche dopo retry/transient failure.

### TODO-007 — Implementare la cancellazione completa nel modulo pranzo

**Evidenza:** il backend espone `DELETE /api/lunch/bookings/{id}`, ma `LunchPage` non presenta riepilogo/cancellazione. Non esiste recupero della prenotazione al refresh. La cancellazione locale di una prenotazione partner non invia alcun comando di annullamento al gateway.

**Impatto:** il requisito POC di prenotazione e cancellazione non è completo end-to-end e lo snapshot Spotly può divergere dal locale.

**Completamento:** UI di riepilogo/cancellazione, query della prenotazione attiva, comando partner mock di cancellazione idempotente, aggiornamento posti e SignalR, test per cancellazione gratuita e no-show.

### TODO-008 — Esercitare davvero RBAC e identità nella demo

**Evidenza:** il frontend usa sempre `DEV_USER` (`authStore.ts`) e non chiama `/api/me`. Le chiamate browser non inviano gli header dev richiesti dal backend. MSW non applica le policy: per esempio il pulsante Facility di tick è disponibile in dev a un utente Manager e viene accettato dal mock.

**Impatto:** le policy backend sono presenti, ma la demo non le attraversa; è possibile mostrare un comportamento diverso da quello reale.

**Completamento:** login/role switch mock esplicito, identità ottenuta da `/api/me`, credenziali/header dev gestiti dal proxy, test E2E 401/403/200 per Dipendente/Manager/Facility/Admin.

## P1 — correttezza e robustezza

### TODO-009 — Correggere le finestre temporali di check-in

**Evidenza:** `CheckInAsync` verifica solo che l'istante sia precedente alla deadline; non verifica data o apertura della finestra. L'audit ha creato una prenotazione per domani ed eseguito il check-in oggi ottenendo `204`. Una prenotazione creata oggi dopo le 09:30 UTC viene invece rilasciata quasi subito come no-show.

**Completamento:** configurare apertura e chiusura check-in, rifiutare check-in anticipato/tardivo e impedire prenotazioni same-day già scadute.

### TODO-010 — Usare timezone della sede e `TimeProvider` in modo uniforme

**Evidenza:** regole e repository mescolano `TimeProvider` e `DateTime.UtcNow`; frontend inizializza la data con `toISOString()`. Cutoff delle 08:00/09:30 e “oggi” sono quindi UTC, non Europe/Rome.

**Completamento:** introdurre timezone sede configurabile, servizio data/ora unico e test sui cambi giorno/ora legale.

### TODO-011 — Recuperare correttamente i booking partner rimasti pending

**Evidenza:** `RestaurantLiveService.BookAsync` crea prima booking e `PendingSeats`, poi chiama il gateway senza compensazione (`RestaurantLiveService.cs:46-63`). Se il gateway lancia timeout/cancellazione/errore, booking e posto pending restano bloccati. Il codice demo `TIMEOUT` è una risposta immediata, non testa questa condizione.

**Completamento:** timeout reale, stato failed/expired, compensazione idempotente, retry controllato e job di recupero dei pending scaduti.

### TODO-012 — Ordinare atomicamente tutti gli aggiornamenti ristorante

**Evidenza:** conferme e cancellazioni cambiano i posti ma non incrementano una sequenza autorevole; il frontend applica qualsiasi evento senza confronto della sequenza (`LunchPage.tsx:20-22`). Un messaggio periodico in ritardo può sovrascrivere il risultato di una prenotazione.

**Completamento:** una versione monotona per ogni mutazione, controllo sequence lato client e test di eventi fuori ordine.

### TODO-013 — Correlare feed SignalR a sede e data

**Evidenza:** `RestaurantPartnerMessage` non salva `BookingDate`; `RestaurantMessageReceived` non la trasporta e viene inviato a `Clients.All` (`RestaurantLiveService.cs:41-42`). La query frontend del feed non include la data nella key.

**Impatto:** cambiando data si possono vedere messaggi di altre date; in un'evoluzione multi-sede gli eventi verrebbero mischiati.

**Completamento:** aggiungere data/sede al modello e al DTO, pubblicare nel gruppo corretto e filtrare/query-key per data.

### TODO-014 — Rendere resilienti i background service

**Evidenza:** poller ristoranti e lifecycle booking non isolano le eccezioni del singolo tick. Un errore repository/SignalR può terminare il `BackgroundService` e, con la configurazione standard, fermare l'host.

**Completamento:** gestione eccezioni per ciclo, retry/backoff, log senza PII, cancellation corretta e health/metriche dedicate.

### TODO-015 — Mostrare errori e stato real-time nel frontend

**Evidenza:** gli errori iniziali di SignalR vengono ignorati (`useSignalR.ts:26-31,55-57`). Le query ristoranti, messaggi e lunch box non mostrano stati errore completi. L'interfaccia continua a dichiarare “in tempo reale” con connessione assente.

**Completamento:** stato connected/reconnecting/offline, retry iniziale esplicito, fallback polling dichiarato, error state con retry per ogni query.

### TODO-016 — Completare il mock Teams/calendario e il pulsante team

**Evidenza:** `MockTeamsCollaborationProvider` ignora data e intervallo (`MockTeamsCollaborationProvider.cs:19-23`), quindi il match è identico per ogni giorno. Il pulsante “Prenota per il team” (`DashboardPage.tsx:84`) non ha alcuna azione.

**Completamento:** dataset mock variabile e deterministico per data/finestra; implementare un flusso coerente o rimuovere/disabilitare il CTA con etichetta “non incluso nella demo”.

### TODO-017 — Ripristinare l'easter egg dei gattini

**Evidenza:** README dichiara Konami code e cinque click sul logo, ma nel frontend non esiste `CatRain`, un listener Konami o un contatore click. `Navbar.tsx:17,26` rende il logo senza trigger.

**Impatto:** viola una proibizione esplicita di `AGENTS.md`.

**Completamento:** integrare il componente previsto dalla skill `spotly-cats` con entrambi i trigger e test ridotto-motion/accessibilità.

### TODO-018 — Validare lo schema Entra/Easy Auth prima della sostituzione dei mock

**Evidenza:** l'handler di produzione decodifica direttamente `X-MS-CLIENT-PRINCIPAL`, rimappa solo ruolo e nome e `CurrentUser.Id` accetta solo `oid` o `NameIdentifier`. Non esiste un test con un principal Easy Auth realistico.

**Completamento:** fixture del payload reale, mapping esplicito dell'object ID e dei ruoli, rifiuto sicuro dei claim mancanti, test del confine che impedisce header spoofing quando Easy Auth non è davanti all'app.

## P2 — qualità, test e manutenibilità

### TODO-019 — Aumentare copertura sui casi di rischio

Mancano test sufficienti per:

- API lock e lock su data futura;
- concorrenza desk/lunch e concorrenza tra repository/istanze;
- cancellazione e R-09 prima/dopo cutoff;
- check-in anticipato/tardivo e release;
- R-02 lunch box, R-06 end-to-end e catalogo inesistente;
- timeout/malformed/correlation mismatch del partner;
- eccezioni nei background service;
- SignalR e sequenze fuori ordine;
- endpoint `me` per tutti i moduli;
- frontend unit/component test e un E2E integrato senza MSW.

Target iniziale consigliato: branch coverage API almeno 60% sui moduli POC, con soglia CI.

### TODO-020 — Integrare gli E2E nel toolchain del progetto

**Evidenza:** `package.json` non contiene script test/E2E; gli script Python dipendono da Playwright installato globalmente e non vengono eseguiti automaticamente.

**Completamento:** comando riproducibile documentato, dipendenze versionate, avvio coordinato FE/BE e job CI. Python resta solo test harness, non runtime applicativo.

### TODO-021 — Aggiungere error boundary e route 404

**Evidenza:** `App.tsx` non contiene route catch-all né error boundary. Un errore render/fetch non gestito può lasciare una pagina vuota o una route sconosciuta con shell incompleta.

### TODO-022 — Eliminare dipendenze runtime esterne dalla rete per la demo

**Evidenza:** `index.css:1` carica font Google e Material Symbols da CDN. In rete limitata la UI perde font e icone.

**Completamento:** self-host degli asset con licenze incluse e fallback verificato offline.

### TODO-023 — Migliorare health check e startup failure

**Evidenza:** `/health` verifica solo che il processo risponda; non controlla database o pipeline real-time. Il catch top-level logga l'errore fatale senza impostare esplicitamente un exit code non-zero (`Program.cs:104-107`).

**Completamento:** readiness separata, check dipendenze selezionate, exit code di errore e test di startup configuration failure.

### TODO-024 — Uniformare error contract e validazione

**Evidenza:** alcuni endpoint restituiscono `BadRequest()` vuoto, altri `ValidationProblem`, altri oggetti `{ error }`. Le date dichiarano formato `yyyy-MM-dd` ma usano `DateOnly.TryParse` invece di `TryParseExact`.

**Completamento:** Problem Details coerente, codici dominio stabili, parsing invariant esatto e mapping uniforme 400/403/404/409/422.

### TODO-025 — Aggiornare documentazione operativa frontend

**Evidenza:** `frontend/README.md` è ancora il testo del template Vite e non spiega MSW, backend, auth dev, SignalR o scenari demo.

**Completamento:** guida unica per modalità mock e integrata, utenti/ruoli demo, porte, scenari ristorante/team e troubleshooting.

## Copertura delle regole inderogabili

| Regola | Stato audit | Nota |
|---|---|---|
| R-01 | Parziale | Applicata nei repository; garanzia SQL/multi-istanza e stato FE da completare. |
| R-02 | Parziale | Backend applica la finestra; frontend propone date/azioni non eleggibili e dati pranzo mancanti nel futuro. |
| R-03 | Critico | Lock valido solo nel processo e sempre sulla data odierna; mancano vincoli SQL. |
| R-04 | Parziale | Release automatica presente per parking/desk; finestre check-in errate e test incompleti. |
| R-05 | Non valutata lato mappe | Esclusa la parte assegnata all'altro agente. |
| R-06 | Parziale | Regola backend presente; UX e scenario demo non coerenti. |
| R-07 | Parziale | Vincolo reparto singolo presente; quote aggregate non modellate. |
| R-08 | Coperta backend | Idoneità speciali verificata sia al lock sia alla conferma; UI mappe esclusa. |
| R-09 | Parziale | Stato no-show applicato in cancellazione; mancano test completi e timezone sede. |

## Elementi verificati e già adeguati

- Minimal API, record DTO, FluentValidation esplicita e policy RBAC sugli endpoint esistenti.
- Identità della prenotazione derivata dal principal, non dal body.
- Nessun numero WhatsApp esposto nei DTO o nei log esaminati.
- Locali filtrati da database per `IsActive` e `WhatsAppNumber` valorizzato.
- Protocollo partner versionato, idempotenza per message ID e rifiuto delle sequenze stale.
- Mock Entra/Teams/Graph dietro `ICollaborationAvailabilityProvider`; dati calendario minimizzati, senza oggetto/partecipanti.
- Integrazioni esterne dietro interfacce e nessuna chiamata reale nella POC.
