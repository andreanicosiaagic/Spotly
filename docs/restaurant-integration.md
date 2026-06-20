# Integrazione ristoranti — demo Telegram e catalogo SQL

## Flusso realizzabile

1. Spotly legge da SQL solo i locali attivi con `WhatsAppNumber` valorizzato.
2. Ogni 10 minuti `RestaurantAvailabilityPollingService` genera, per la POC, un messaggio Telegram standard per ciascun locale configurato.
3. `RestaurantLiveService` valida il protocollo, applica idempotenza e ordinamento per sequenza, aggiorna lo snapshot SQL e pubblica SignalR.
4. Una prenotazione crea un hold di un posto (`PendingSeats`) in transazione serializzabile.
5. Il gateway Telegram mock restituisce un messaggio `BOOK_RESULT` codificato.
6. Spotly chiude l'hold: `Confirmed` con posti residui autoritativi oppure `Rejected` con codice errore.
7. UI e client connessi ricevono il nuovo contatore tramite `RestaurantAvailabilityChanged`.

## Protocollo v1

Campi separati da `|`; date `yyyy-MM-dd`; timestamp UTC ISO-8601.

```text
SPOTLY|1|AVAIL|<messageId>|<restaurantId>|<date>|<availableSeats>|<sequence>|<sentAtUtc>
SPOTLY|1|BOOK|<correlationId>|<restaurantId>|<date>|1
SPOTLY|1|BOOK_RESULT|<correlationId>|<restaurantId>|<date>|OK|<remainingSeats>|<partnerReference>
SPOTLY|1|BOOK_RESULT|<correlationId>|<restaurantId>|<date>|FULL|0|-
```

Codici supportati: `OK`, `FULL`, `CLOSED`, `INVALID_DATE`, `TIMEOUT`; Spotly produce `MALFORMED`, `NOT_CONFIGURED` e `LOCAL_REJECTED` quando il rifiuto avviene prima o durante la validazione.

`messageId` garantisce idempotenza. `sequence` è monotona per locale/data: un messaggio con sequenza minore o uguale allo snapshot è registrato come stale e non modifica i posti.

## Persistenza SQL

Configurare esclusivamente tramite ambiente:

```powershell
$env:Database__Provider = 'SqlServer'
$env:ConnectionStrings__Spotly = 'Server=tcp:<server>;Database=SpotlyDB;Authentication=Active Directory Default;Encrypt=True;'
dotnet run --project backend/Spotly.Api
```

Per la demo su un database vuoto EF Core crea lo schema con `EnsureCreatedAsync`. Le tabelle rilevanti sono:

- `Restaurants`: anagrafica, capacità, numero WhatsApp e chat Telegram demo;
- `RestaurantAvailabilities`: snapshot posti, hold, sequenza e timestamp per locale/data;
- `RestaurantPartnerMessages`: inbox/audit dei messaggi disponibilità;
- `LunchBookings`: correlation ID, stato partner, codice, riferimento e posti residui.

Il numero WhatsApp non viene incluso nei DTO né nei log. La connection string non deve essere salvata in `appsettings.json`.

## Configurazione locale

Un locale partecipa se:

```text
IsActive = true AND WhatsAppNumber IS NOT NULL AND WhatsAppNumber <> ''
```

`TelegramChatId` è routing opzionale della demo; non sostituisce l'identificativo stabile `RestaurantId`.

## Comandi demo

Gli endpoint richiedono ruolo `Facility` o `Admin`:

```http
POST /api/lunch/demo/tick?date=2026-06-20
POST /api/lunch/demo/restaurants/R01/availability?date=2026-06-20
Content-Type: application/json

{"availableSeats": 7}
```

Per predisporre l'esito della prenotazione successiva:

```http
POST /api/lunch/demo/restaurants/R01/next-booking-outcome
Content-Type: application/json

{"code": "FULL"}
```

Il pulsante `Ricevi aggiornamenti demo` usa il tick e mostra eventi di più locali senza attendere dieci minuti. L'intervallo reale resta configurato da `RestaurantMessaging:PollingIntervalMinutes`.

## Passaggio a canali reali

Sostituire `MockTelegramRestaurantGateway` con un adapter `IRestaurantMessagingGateway`. Webhook, token Telegram/WhatsApp, retry e firma del provider devono restare nell'adapter; prenotazioni, idempotenza, sequenze e SignalR non cambiano.
