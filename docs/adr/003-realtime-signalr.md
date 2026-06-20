# ADR 003 — Real-time con SignalR e Lock Ottimistico

**Data:** 20/06/2026  
**Stato:** Accepted  
**Contesto:** Requisito M1-02 (disponibilità real-time), M0 (no doppie prenotazioni — R-03)

---

## Problema

Più utenti vedono la stessa mappa di disponibilità. Quando uno prenota, gli altri devono vedere la risorsa occuparsi **senza refresh**. Inoltre occorre prevenire la doppia assegnazione della stessa risorsa (race condition).

## Decisione

### Real-time: SignalR Hub

Un `AvailabilityHub` trasmette aggiornamenti di disponibilità a tutti i client connessi alla stessa sede/giorno:

```
Client A prenota Posto P1
  → Backend valida, acquisisce lock, persiste
  → Notifica tutti i client: "P1 ora è OCCUPATO"
  → Client B vede la mappa aggiornarsi senza refresh
```

- I client sottoscrivono un gruppo `availability:{sedeId}:{data}`
- Il payload è minimale: `{ resourceId, resourceType, newStatus }`
- La POC usa SignalR in-process (no Azure SignalR Service); la transizione è configurabile

### Lock Ottimistico (R-03)

Per prevenire doppie prenotazioni durante la finestra di conferma:

1. **Tentativo di lock:** quando un utente avvia la prenotazione, la risorsa entra in stato `PENDING_CONFIRMATION` per N minuti (configurabile, default 3 min)
2. **Lock atomico:** l'acquisizione del lock usa un `SemaphoreSlim` per risorsa + controllo di stato in unica transazione
3. **Scadenza:** un background service rilascia i lock scaduti e notifica i client via SignalR
4. **Conferma:** solo il client che detiene il lock può confermare la prenotazione

```
Stato risorsa: LIBERA → PENDING (lock) → OCCUPATA (confermata)
                                       ↓ timeout
                               LIBERA (lock scaduto)
```

## Alternative scartate

- **Polling** — troppo carico, latenza inaccettabile (NFR: aggiornamenti < 1s)
- **SSE (Server-Sent Events)** — solo unidirezionale, non permette messaggi client→server
- **Pessimistic locking (DB row lock)** — non applicabile con EF InMemory; overkill per POC

## Conseguenze

- Il FE deve gestire lo stato `PENDING_CONFIRMATION` nelle mappe (colore diverso da LIBERA e OCCUPATA)
- Il lock N-minuti deve essere visibile all'utente ("Risorsa riservata per te per 3:00")
- Alla riconnessione SignalR, il client deve richiedere lo snapshot completo di disponibilità
