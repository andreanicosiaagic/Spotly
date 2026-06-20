# Spotly Azure deployment

Prerequisites: Azure CLI, Azure Developer CLI, Bicep CLI, and permission to create resources and role assignments in the selected subscription.

```powershell
azd auth login
azd env new dev
azd env set AZURE_LOCATION westeurope
azd env set ENTRA_CLIENT_ID <app-registration-client-id>
azd up
```

The Entra app registration must define the `Dipendente`, `Manager`, `Facility`, and `Admin` app roles. AZD supplies the current tenant ID and the client ID is mandatory; provisioning does not create or weaken identity configuration.

The Bicep deployment contains no source-controlled credentials. The App Service managed identity receives only the `SignalR App Server` role on the SignalR resource and authenticates without access keys.
