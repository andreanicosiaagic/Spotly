# ADR 003 — Real-time con SignalR e Lock Ottimistico

**Data:** 20/06/2026  
**Stato:** Accepted *(aggiornato: allineato a architecture-poc.md)*  
**Contesto:** Requisito M1-02 (disponibilità real-time), M0 (no doppie prenotazioni — R-03)

---

## Problema

Più utenti vedono la stessa mappa di disponibilità. Quando uno prenota, gli altri devono vedere la risorsa occuparsi **senza refresh**. Inoltre occorre prevenire la doppia assegnazione della stessa risorsa (race condition).

## Decisione

### Real-time: Azure SignalR Service F1 + Hub

La POC usa **Azure SignalR Service F1** (managed) come backplane. Il backend espone un `AvailabilityHub` che delega la distribuzione dei messaggi al servizio gestito:

```
Client A prenota Posto P1
  → Backend valida, acquisisce lock, persiste su Azure SQL
  → Notifica Azure SignalR Hub "parking-{locationId}"
  → Azure SignalR Service distribuisce a tutti i client connessi
  → Client B vede la mappa aggiornarsi senza refresh (< 1s)
```

- I client sottoscrivono un gruppo `availability:{sedeId}:{data}`
- Il payload è minimale: `{ resourceId, resourceType, newStatus }`
- **Azure SignalR F1**: 20 connessioni simultanee, 20.000 messaggi/giorno — sufficiente per la demo hackathon
- La connection string di SignalR è in **Key Vault** (KV Reference in App Service) — mai in codice

```csharp
// Program.cs
builder.Services.AddSignalR().AddAzureSignalR(
    builder.Configuration["Azure:SignalR:ConnectionString"]); // letto da KV ref

// Hub
public class AvailabilityHub : Hub
{
    public async Task JoinGroup(string sedeId, string data) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, $"availability:{sedeId}:{data}");
}
```

### Lock Ottimistico (R-03)

Per prevenire doppie prenotazioni durante la finestra di conferma:

1. **Tentativo di lock:** quando un utente avvia la prenotazione, la risorsa entra in stato `PENDING_CONFIRMATION` per N minuti (configurabile, default 3 min)
2. **Lock atomico:** l'acquisizione del lock usa una transazione SQL con `UPDATE ... WHERE LockedUntil IS NULL OR LockedUntil < GETUTCDATE()` — atomica grazie all'isolation level del DB
3. **Scadenza:** un background service (`IHostedService`) rilascia i lock scaduti (`UPDATE ... SET LockedUntil = NULL WHERE LockedUntil < GETUTCDATE()`) e notifica i client via Azure SignalR
4. **Conferma:** solo il client che detiene il lock può confermare la prenotazione

```
Stato risorsa: LIBERA → PENDING (lock) → OCCUPATA (confermata)
                                       ↓ timeout
                               LIBERA (lock scaduto)
```

## Alternative scartate

- **Polling** — troppo carico, latenza inaccettabile (NFR: aggiornamenti < 1s)
- **SSE (Server-Sent Events)** — solo unidirezionale, non permette messaggi client→server
- **Pessimistic locking (DB row lock)** — overkill per POC; preferita la soluzione `LockedUntil` + `LockedByUserId` a livello applicativo

## Conseguenze

- **Azure SignalR F1** come servizio managed — nessun hub in-process
- La connection string SignalR è in Key Vault (mai in appsettings.json)
- Il FE deve gestire lo stato `PENDING_CONFIRMATION` nelle mappe (colore diverso da LIBERA e OCCUPATA)
- Il lock N-minuti deve essere visibile all'utente ("Risorsa riservata per te per 3:00")
- Le colonne `LockedUntil DATETIME2 NULL` e `LockedByUserId NVARCHAR(100) NULL` sono presenti in `ParkingBookings` e `DeskBookings` (vedi schema in architecture-poc.md)
- Alla riconnessione SignalR, il client deve richiedere lo snapshot completo di disponibilità
