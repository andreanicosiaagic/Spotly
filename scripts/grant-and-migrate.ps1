# Grant Azure SQL data-plane access to the App Service managed identity.
#
# Called automatically by the azd postprovision hook (see azure.yaml).
# Skips silently when deploySql=false (SQL_SERVER not set in azd env).
#
# Prerequisites:
#   - Azure CLI logged in as the Entra SQL admin (SQL_ADMIN_LOGIN)
#   - az extension rdbms-connect installed (auto-installed below if missing)
#
# The app uses DefaultAzureCredential → system-assigned managed identity.
# Schema creation is handled by EnsureCreatedAsync() at startup; no EF migrations needed.

$ErrorActionPreference = 'Stop'

# Load azd environment variables
azd env get-values | ForEach-Object {
    $name, $value = $_.Split('=', 2)
    Set-Item "env:$name" $value.Trim('"')
}

# Skip when SQL was not provisioned
if (-not $env:SQL_SERVER) {
    Write-Host "SQL_SERVER not set — SQL provisioning skipped (deploySql=false)."
    exit 0
}

$AppName = if ($env:SERVICE_API_RESOURCE_NAME) { $env:SERVICE_API_RESOURCE_NAME } else { $env:SERVICE_API_NAME }
if (-not $AppName) {
    throw "Neither SERVICE_API_RESOURCE_NAME nor SERVICE_API_NAME is set in the azd environment."
}

Write-Host "Granting SQL access to managed identity: $AppName"
Write-Host "  Server  : $env:SQL_SERVER"
Write-Host "  Database: $env:SQL_DATABASE"

# Ensure rdbms-connect extension is available
az extension show --name rdbms-connect 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Installing az extension rdbms-connect..."
    az extension add --name rdbms-connect --yes
    if ($LASTEXITCODE -ne 0) { throw "Failed to install rdbms-connect extension." }
}

$SqlQuery = @"
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = '$AppName')
  CREATE USER [$AppName] FROM EXTERNAL PROVIDER;

IF NOT EXISTS (
  SELECT 1 FROM sys.database_role_members drm
  JOIN sys.database_principals r ON drm.role_principal_id = r.principal_id
  JOIN sys.database_principals m ON drm.member_principal_id = m.principal_id
  WHERE r.name = 'db_datareader' AND m.name = '$AppName'
)
  ALTER ROLE db_datareader ADD MEMBER [$AppName];

IF NOT EXISTS (
  SELECT 1 FROM sys.database_role_members drm
  JOIN sys.database_principals r ON drm.role_principal_id = r.principal_id
  JOIN sys.database_principals m ON drm.member_principal_id = m.principal_id
  WHERE r.name = 'db_datawriter' AND m.name = '$AppName'
)
  ALTER ROLE db_datawriter ADD MEMBER [$AppName];

IF NOT EXISTS (
  SELECT 1 FROM sys.database_role_members drm
  JOIN sys.database_principals r ON drm.role_principal_id = r.principal_id
  JOIN sys.database_principals m ON drm.member_principal_id = m.principal_id
  WHERE r.name = 'db_ddladmin' AND m.name = '$AppName'
)
  ALTER ROLE db_ddladmin ADD MEMBER [$AppName];
"@

az sql db query `
  --server $env:SQL_SERVER `
  --database $env:SQL_DATABASE `
  --resource-group $env:AZURE_RESOURCE_GROUP `
  --auth-mode ActiveDirectoryDefault `
  --queries $SqlQuery

if ($LASTEXITCODE -ne 0) { throw "Failed to grant SQL access to managed identity '$AppName'." }

Write-Host "SQL access granted successfully."
