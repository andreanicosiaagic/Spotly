targetScope = 'subscription'

@minLength(1)
param environmentName string
param location string = 'northeurope'
@description('Microsoft Entra tenant that issues Spotly identities. Defaults to the deployment subscription tenant.')
param entraTenantId string = subscription().tenantId
@description('App registration client ID configured with Spotly app roles. Leave empty to skip Easy Auth (default for POC).')
param entraClientId string = ''
param allowedOrigin string = ''
param appServicePlanSku string = 'B2'
@description('UPN or display name of the Entra principal that will be SQL Server admin.')
param sqlAdminLogin string = ''
@description('Object ID of the Entra principal set as SQL admin.')
param sqlAdminObjectId string = ''
@description('Principal type of the SQL admin: User for interactive, Application for CI/CD.')
@allowed(['User', 'Group', 'Application'])
param sqlAdminPrincipalType string = 'Application'

var normalizedEnvironment = toLower(replace(environmentName, '-', ''))
var suffix = uniqueString(subscription().id, environmentName, location)
var resourceGroupName = 'rg-${environmentName}'
var tags = {
  application: 'spotly'
  environment: environmentName
  workload: 'poc'
  'azd-env-name': environmentName
}

resource resourceGroup 'Microsoft.Resources/resourceGroups@2024-11-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

module resources 'resources.bicep' = {
  name: 'spotly-resources-${suffix}'
  scope: resourceGroup
  params: {
    location: location
    namePrefix: 'spotly-${take(normalizedEnvironment, 12)}'
    suffix: suffix
    tags: tags
    entraTenantId: entraTenantId
    entraClientId: entraClientId
    allowedOrigin: allowedOrigin
    appServicePlanSku: appServicePlanSku
    sqlAdminLogin: sqlAdminLogin
    sqlAdminObjectId: sqlAdminObjectId
    sqlAdminPrincipalType: sqlAdminPrincipalType
  }
}

output AZURE_LOCATION string = location
output AZURE_RESOURCE_GROUP string = resourceGroup.name
output SERVICE_API_RESOURCE_NAME string = resources.outputs.apiName
output SERVICE_API_URI string = resources.outputs.apiUri
output APPLICATIONINSIGHTS_CONNECTION_STRING string = resources.outputs.applicationInsightsConnectionString
output AZURE_SQL_SERVER_FQDN string = resources.outputs.sqlServerFqdn
output SQL_SERVER string = resources.outputs.sqlServerName
output SQL_DATABASE string = resources.outputs.sqlDatabaseName
output AZURE_KEY_VAULT_URI string = resources.outputs.keyVaultUri
output AZURE_STORAGE_ACCOUNT_NAME string = resources.outputs.storageAccountName
