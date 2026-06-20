---
name: pre-tool-call
description: Hook eseguito PRIMA di ogni tool call dell'agente. Verifica condizioni di sicurezza e correttezza.
trigger: pre_tool_call
---

# Pre-Tool-Call Hook — Spotly

Esegui questi controlli **prima** di qualsiasi operazione di scrittura (edit, create, bash/powershell con side effect).

## 1. Nessun segreto reale nel codice

Prima di scrivere un file, verifica che non contenga:
- API keys, client secrets, connection strings reali
- Password hardcodate
- Token di accesso reali

Se stai per scrivere una credenziale, usa invece:
- `import.meta.env.VITE_*` (FE) o `Environment.GetEnvironmentVariable(...)` / `appsettings.Development.json` (BE)

## 2. Rispetto dei mock boundaries (POC)

Prima di implementare una chiamata verso un servizio esterno, verifica che esista un'interfaccia mock in `Spotly.Domain/` e che tu stia usando quella, NON l'SDK reale.

Integrazioni che devono rimanere mockate nella POC:
- Entra ID / MSAL → usa `IIdentityProvider` / `MockIdentityProvider`
- Microsoft Graph → usa `ICalendarIntegration` / `MockCalendarIntegration`
- Badge/tornelli → usa `IAccessControlSystem` / `MockAccessControlSystem`
- Partner ristorazione → usa `IRestaurantPartner` / `MockRestaurantPartner`
- Welfare/buoni pasto → usa `IWelfareProvider` / `MockWelfareProvider`

## 3. Nessuna PII nei log

Prima di aggiungere una riga di log, verifica che non contenga:
- Email degli utenti
- Nomi completi
- ID personali non anonimizzati
- Posizioni GPS

Usa invece: `userId` opaco, `resourceId`, `bookingId`.

## 4. RBAC non rimosso

Prima di modificare un endpoint, verifica che la verifica del ruolo sia ancora presente.
