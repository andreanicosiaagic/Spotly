# ADR 005 — Infrastruttura Azure POC

**Data:** 20/06/2026  
**Stato:** Accepted  
**Contesto:** Infrastruttura Azure per il POC Spotly (hackathon App-in-a-Day). Deployabile in < 30 min con Bicep IaC.

---

## Problema

Il POC richiede un ambiente Azure funzionante e sicuro per i 3 moduli (M1 Parcheggio, M2 Postazioni, M3 Pranzo) con SSO Entra ID, real-time SignalR, persistenza SQL, piantine su blob, e observability — senza complessità enterprise.

## Decisione

### Componenti Azure POC

| Risorsa | SKU / Tier | Motivazione |
|---|---|---|
| **Resource Group** | `rg-spotly-poc` | Isolamento, cleanup facile dopo demo |
| **App Service Plan** | B2 (2 vCore, 3.5 GB) | Hosting Web App + API; scaling manuale sufficiente per demo |
| **Azure App Service** | — | `spotly-poc-{suffix}.azurewebsites.net`; Easy Auth Entra ID; HTTPS only; TLS 1.2 min |
| **Azure SQL Database** | Serverless GP_S_Gen5 (0.5–4 vCore) | SpotlyDB; cold start accettabile per demo; costo ~€0 idle |
| **Azure SQL Server** | — | `spotly-poc-{suffix}-sql.database.windows.net`; Entra-only auth (no SQL auth) |
| **Azure SignalR Service** | Free F1 (20 conn, 20K msg/die) | Real-time mappe; sufficiente per la demo hackathon |
| **Storage Account** | Standard LRS | Piantine SVG/PNG; container `floor-plans/` privato |
| **Azure Key Vault** | Standard | SignalR connection string, eventuali secrets futuri |
| **Application Insights** | — | APM, request tracing, exception tracking |
| **Log Analytics Workspace** | PerGB2018 | Centralizzazione log (App Insights dipende da questo) |
| **Virtual Network** | 10.1.0.0/16 | Isolamento rete |
| **Subnet app** | snet-app 10.1.1.0/24 | VNet Integration per App Service |
| **Subnet PE** | snet-pe 10.1.2.0/24 | Private Endpoints (SQL, KV, Storage) |
| **Private Endpoint SQL** | — | Accesso privato a Azure SQL |
| **Private Endpoint KV** | — | Accesso privato a Key Vault |
| **Private Endpoint Storage** | — | Accesso privato a Storage Account |
| **Private DNS Zone SQL** | `privatelink.database.windows.net` | Risoluzione DNS privata nel VNet |
| **Private DNS Zone KV** | `privatelink.vaultcore.azure.net` | Risoluzione DNS privata nel VNet |
| **Private DNS Zone Storage** | `privatelink.blob.core.windows.net` | Risoluzione DNS privata nel VNet |

---

## Sicurezza POC

| Controllo | Implementazione |
|---|---|
| Autenticazione | Entra ID Easy Auth (OIDC, cookie sicuro) |
| Autorizzazione | App Roles nel token JWT (Dipendente, Manager, Facility, Admin) |
| HTTPS + TLS 1.2 | Forzato su App Service |
| FTPS | Disabilitato |
| Managed Identity | App Service → SQL (Storage Blob Data Reader, Key Vault Secrets User) |
| Secrets | Solo in Key Vault via KV Reference — mai in codice o appsettings |
| VNet Integration | App Service outbound → VNet (vnetRouteAllEnabled: true) |
| Private Endpoints | SQL + KV + Storage accessibili solo via IP privato dalla VNet |
| Public Network Access | **Enabled** su SQL/KV/Storage nel POC — per permettere deploy da GitHub Actions |
| Blob pubblici | Disabilitati su Storage Account |
| Soft Delete + Purge Protection | Abilitati su Key Vault |

> 📌 **Nota POC vs Enterprise:** `publicNetworkAccess: Enabled` è necessario per il deploy da pipeline GitHub Actions. In Enterprise si usa un self-hosted runner nella VNet oppure un deployment slot con network isolation.

---

## Identità e accessi (RBAC Azure)

| Identità | Ruolo assegnato | Risorsa |
|---|---|---|
| App Service Managed Identity | `Storage Blob Data Reader` | Storage Account |
| App Service Managed Identity | `Key Vault Secrets User` | Key Vault |
| App Service Managed Identity | `db_owner` (via Entra SQL admin) | Azure SQL Database |
| Developer (deploy) | `Contributor` su `rg-spotly-poc` | Resource Group |
| GitHub Actions Service Principal | `Contributor` su `rg-spotly-poc` | Resource Group |

---

## Struttura Bicep IaC

```
infra/
  main.bicep               ← orchestrazione (chiama tutti i moduli)
  main.bicepparam          ← parametri ambiente (suffix, location, tenantId, appClientId)
  modules/
    networking.bicep        ← VNet, subnets, Private DNS Zones
    app-service.bicep       ← App Service Plan + App + Easy Auth + VNet Integration
    sql.bicep               ← SQL Server (Entra auth only) + Database Serverless + PE
    signalr.bicep           ← Azure SignalR F1
    storage.bicep           ← Storage Account + container floor-plans + PE
    keyvault.bicep          ← Key Vault + soft delete + RBAC + PE
    monitoring.bicep        ← Application Insights + Log Analytics
    private-endpoints.bicep ← PE SQL + PE KV + PE Storage + DNS Zone links
```

### Deploy command

```bash
# Login e selezione subscription
az login
az account set --subscription "<subscription-id>"

# Deploy infra completa (< 30 min)
az deployment group create \
  --resource-group rg-spotly-poc \
  --template-file infra/main.bicep \
  --parameters infra/main.bicepparam \
  --name deploy-$(date +%Y%m%d%H%M)
```

---

## Flusso rete (POC)

```
App Service (snet-app 10.1.1.0/24)
    │ VNet Integration (vnetRouteAllEnabled: true)
    │ Tutto il traffico outbound → VNet
    ▼
Private Endpoints (snet-pe 10.1.2.0/24)
    ├── PE SQL    → spotly-poc-xxxxx-sql.database.windows.net → 10.1.2.4
    ├── PE KV     → kv-spotlypocxxxxx.vault.azure.net         → 10.1.2.5
    └── PE Storage→ spotlypocxxxxxst.blob.core.windows.net    → 10.1.2.6

Private DNS Zones (linked to VNet):
    ├── privatelink.database.windows.net    → sovrascrive DNS pubblico
    ├── privatelink.vaultcore.azure.net     → sovrascrive DNS pubblico
    └── privatelink.blob.core.windows.net   → sovrascrive DNS pubblico
```

---

## Alternative scartate

| Alternativa | Motivazione scarto |
|---|---|
| Container Apps | Overkill per POC; B2 App Service è più veloce da configurare |
| Azure Functions | Architettura stateful (SignalR Hub) non si adatta bene a Functions |
| Azure Static Web Apps + separate API | Aggiunge complessità di gestione CORS; B2 ospita tutto insieme |
| SQL Basic tier | Non supporta Serverless; GP_S_Gen5 è la scelta minima cost-effective |

---

## Conseguenze

- Il Bicep IaC è la fonte di verità per l'infrastruttura — nessuna risorsa creata manualmente
- GitHub Actions workflow usa un Service Principal con `Contributor` sul resource group
- La skill `azure-deploy` guida il setup del workflow di deploy
- La transizione Enterprise richiede: Container Apps, PE full-private, multi-region, IoT Hub (vedi architecture-enterprise.md)
