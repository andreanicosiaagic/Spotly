---
name: post-file-edit
description: Hook eseguito DOPO ogni modifica a un file. Suggerisce azioni di follow-up.
trigger: post_file_edit
---

# Post-File-Edit Hook — Spotly

Dopo aver modificato un file, valuta se sono necessarie queste azioni:

## 1. Suggerisci un test (Domain + Modules)

Se hai modificato un file in uno di questi path:
- `Spotly.Domain/`
- `frontend/src/modules/`
- `frontend/src/hooks/`

→ Verifica che esista un test corrispondente. Se non esiste, **proponi all'utente** di crearlo prima di procedere.

Esempio di prompt da mostrare:
> "Ho modificato `parking/use-booking.ts`. Vuoi che crei anche il test `use-booking.test.ts`?"

## 2. Aggiorna l'ADR se cambi un'architettura

Se hai modificato:
- Il pattern di un repository o hub
- Il modo in cui funziona il real-time
- La strategia di mock

→ Ricorda di aggiornare l'ADR pertinente in `docs/adr/`.

## 3. Verifica lock ottimistico (R-03)

Se hai modificato la logica di booking (`BookingService`, endpoint POST `/bookings`):
→ Verifica che il lock ottimistico sia ancora presente e funzionante.

## 4. Non dimenticare l'easter egg

Se hai modificato `App.tsx`, `layout` o il logo di Spotly:
→ Verifica che il componente `<CatRain />` e il suo trigger siano ancora presenti.
