# Azure Deployment Plan

> **Status:** Ready for Validation

Generated: 2026-06-20

## 1. Project Overview

**Goal:** Complete the Spotly POC backend and prepare repeatable Azure deployment artifacts.

**Path:** Modernize existing application by adding Azure support.

## 2. Requirements

| Attribute | Value |
|---|---|
| Classification | POC |
| Scale | Small, under 1,000 users |
| Budget | Cost-optimized |
| Subscription | Deployment-time AZD context; no deployment authorized in this task |
| Location | Parameterized, default `westeurope` |
| Compliance | No PII in technical logs; no source-controlled secrets |

## 3. Components Detected

| Component | Type | Technology | Path |
|---|---|---|---|
| api | API and SignalR hub | ASP.NET Core 10 / C# 14 | `backend/Spotly.Api` |
| domain | Domain model and policy | .NET 10 | `backend/Spotly.Domain` |
| infrastructure | EF Core InMemory and mock integrations | .NET 10 | `backend/Spotly.Infrastructure` |
| web | SPA, not changed by this plan | React 19 / Vite 8 | `frontend` |

No Copilot SDK, Azure Functions, existing `azure.yaml`, IaC, or container markers were detected.

## 4. Recipe Selection

**Selected:** AZD with Bicep.

**Rationale:** Default Azure-first workflow, direct support for a multi-resource POC, environment parameters, and App Service source deployment.

## 5. Architecture

**Stack:** App Service.

| Component | Azure Service | SKU |
|---|---|---|
| Spotly API + SignalR hub | Linux App Service | B1 default, parameterized |
| Realtime backplane | Azure SignalR Service | Free_F1 |
| Central logs | Log Analytics | PerGB2018 |
| APM | Application Insights | workspace-based |

The App Service uses system-assigned managed identity, HTTPS-only, TLS 1.2+, FTPS disabled, health checks, and the required `azd-service-name` tag. Entra Easy Auth requires tenant/client parameters. The managed identity receives the least-privilege `SignalR App Server` role scoped to SignalR and authenticates without keys. EF Core InMemory and mock integrations intentionally avoid external data stores for this POC.

## 6. Provisioning Limit Checklist

This task prepares IaC but does not provision resources or select a subscription. Capacity must be checked by `azure-quotas` against the chosen subscription before `azd provision`.

| Resource Type | Number to Deploy | Expected Total Increment | Limit/Quota | Notes |
|---|---:|---:|---|---|
| `Microsoft.Web/serverfarms` | 1 | +1 | Subscription/region dependent | B1 Linux; validate before deploy |
| `Microsoft.Web/sites` | 1 | +1 | Subscription dependent | App Service app |
| `Microsoft.SignalRService/signalR` | 1 | +1 | Subscription/region dependent | Free_F1 availability must be checked |
| `Microsoft.OperationalInsights/workspaces` | 1 | +1 | Subscription dependent | Log Analytics |
| `Microsoft.Insights/components` | 1 | +1 | Subscription dependent | Application Insights |

**Status:** Preparation-only inventory complete; live quota validation is a deployment prerequisite because no Azure subscription was selected.

## 7. Execution Checklist

### Planning

- [x] Analyze workspace and ADRs
- [x] Gather POC requirements from repository instructions
- [x] Scan components and specialized technology markers
- [x] Select AZD+Bicep recipe and App Service architecture
- [x] Treat the user's “procedi senza ulteriori conferme” as approval to execute this plan

### Execution

- [x] Load App Service generation and security references
- [x] Complete backend implementation
- [x] Generate `azure.yaml` and modular Bicep
- [x] Apply secure App Service and telemetry defaults
- [x] Run functional verification through in-process HTTP tests
- [x] Set status to `Ready for Validation`

### Validation

- [x] Invoke `azure-validate`
- [x] AZD installation and schema review
- [x] Authentication context check (Azure CLI authenticated; AZD login absent)
- [x] Bicep compilation and linting
- [x] Build and test backend
- [x] AZD package validation
- [ ] Live provision preview and Azure Policy validation (requires selected subscription)

### Deployment

- [ ] Select subscription and validate live regional quota
- [ ] Invoke `azure-deploy` only when deployment is explicitly requested

## 8. Validation Proof

| Check | Command Run | Result | Timestamp |
|---|---|---|---|
| Backend build | `dotnet build Spotly.slnx --no-restore` | ✅ Pass, 0 warnings | 2026-06-20T14:00:05+02:00 |
| Backend tests | `dotnet test Spotly.slnx --no-build --no-restore` | ✅ Pass, 17/17 | 2026-06-20T14:00:05+02:00 |
| Bicep compile | `az bicep build --file infra/main.bicep --stdout` | ✅ Pass | 2026-06-20T14:00:05+02:00 |
| ARM validation | `az deployment sub validate ...` with non-secret validation parameters | ✅ Succeeded | 2026-06-20T14:00:05+02:00 |
| ARM what-if | `az deployment sub what-if ... --result-format ResourceIdOnly` | ✅ Pass | 2026-06-20T14:00:05+02:00 |
| AZD package | `azd package --no-prompt` | ✅ Pass | 2026-06-20T14:00:05+02:00 |
| RBAC static review | App Service identity → `SignalR App Server` scoped to SignalR | ✅ Role ID verified from Azure | 2026-06-20T14:00:05+02:00 |

**Validation status:** Static, package, ARM validation, and what-if checks pass. Full `azd provision --preview` and Azure Policy/quota checks remain deployment prerequisites because no deployment environment, Entra client ID, or user-confirmed subscription/location was configured. Per `azure-validate`, status remains `Ready for Validation` rather than `Validated`.

## 9. Files to Generate

| File | Purpose | Status |
|---|---|---|
| `.azure/deployment-plan.md` | Workflow source of truth | Complete |
| `azure.yaml` | AZD service mapping | Complete |
| `infra/main.bicep` | Subscription/resource-group orchestration | Complete |
| `infra/resources.bicep` | App Service, SignalR, and monitoring | Complete |
| `infra/main.parameters.json` | AZD environment parameter mapping | Complete |

## 10. Next Steps

1. Implement and test backend behavior.
2. Generate IaC and validate it locally.
3. Hand off to `azure-validate`; do not deploy in this task.
