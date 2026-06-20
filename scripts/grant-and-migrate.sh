#!/usr/bin/env bash
# Grant Azure SQL data-plane access to the App Service managed identity.
#
# Called automatically by the azd postprovision hook (see azure.yaml).
# Skips silently when deploySql=false (SQL_SERVER not set in azd env).
set -euo pipefail

eval "$(azd env get-values)"

if [ -z "${SQL_SERVER:-}" ]; then
  echo "SQL_SERVER not set — SQL provisioning skipped (deploySql=false)."
  exit 0
fi

APP_NAME="${SERVICE_API_RESOURCE_NAME:-${SERVICE_API_NAME:-}}"
if [ -z "$APP_NAME" ]; then
  echo "ERROR: Neither SERVICE_API_RESOURCE_NAME nor SERVICE_API_NAME is set." >&2
  exit 1
fi

echo "Granting SQL access to managed identity: $APP_NAME"
echo "  Server  : $SQL_SERVER"
echo "  Database: $SQL_DATABASE"

az extension show --name rdbms-connect &>/dev/null || \
  az extension add --name rdbms-connect --yes

az sql db query \
  --server "$SQL_SERVER" \
  --database "$SQL_DATABASE" \
  --resource-group "$AZURE_RESOURCE_GROUP" \
  --auth-mode ActiveDirectoryDefault \
  --queries "
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = '$APP_NAME')
  CREATE USER [$APP_NAME] FROM EXTERNAL PROVIDER;

IF NOT EXISTS (
  SELECT 1 FROM sys.database_role_members drm
  JOIN sys.database_principals r ON drm.role_principal_id = r.principal_id
  JOIN sys.database_principals m ON drm.member_principal_id = m.principal_id
  WHERE r.name = 'db_datareader' AND m.name = '$APP_NAME'
)
  ALTER ROLE db_datareader ADD MEMBER [$APP_NAME];

IF NOT EXISTS (
  SELECT 1 FROM sys.database_role_members drm
  JOIN sys.database_principals r ON drm.role_principal_id = r.principal_id
  JOIN sys.database_principals m ON drm.member_principal_id = m.principal_id
  WHERE r.name = 'db_datawriter' AND m.name = '$APP_NAME'
)
  ALTER ROLE db_datawriter ADD MEMBER [$APP_NAME];

IF NOT EXISTS (
  SELECT 1 FROM sys.database_role_members drm
  JOIN sys.database_principals r ON drm.role_principal_id = r.principal_id
  JOIN sys.database_principals m ON drm.member_principal_id = m.principal_id
  WHERE r.name = 'db_ddladmin' AND m.name = '$APP_NAME'
)
  ALTER ROLE db_ddladmin ADD MEMBER [$APP_NAME];
"

echo "SQL access granted successfully."
