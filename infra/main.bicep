targetScope = 'subscription'

@minLength(1)
param environmentName string
param location string = 'italynorth'
@description('Region for Azure SQL (GP_S_Gen5 may be restricted in some subscriptions).')
param sqlLocation string = 'italynorth'
@description('Set to false to skip SQL Server/DB when the subscription has no SQL quota.')
param deploySql bool = true
@description('Microsoft Entra tenant that issues Spotly identities.')
param entraTenantId string
@description('App registration client ID configured with Spotly app roles.')
param entraClientId string
param allowedOrigin string = ''
param appServicePlanSku string = 'B2'
@description('UPN or display name of the Entra principal that will be SQL Server admin.')
param sqlAdminLogin string
@description('Object ID of the Entra principal set as SQL admin.')
param sqlAdminObjectId string
@description('Principal type of the SQL admin: User for interactive, Application for CI/CD.')
@allowed(['User', 'Group', 'Application'])
param sqlAdminPrincipalType string = 'User'

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
    sqlLocation: sqlLocation
    deploySql: deploySql
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
output AZURE_SQL_SERVER_FQDN string = deploySql ? resources.outputs.sqlServerFqdn : ''
output SQL_SERVER string = deploySql ? resources.outputs.sqlServerName : ''
output SQL_DATABASE string = deploySql ? resources.outputs.sqlDatabaseName : ''
output AZURE_KEY_VAULT_URI string = resources.outputs.keyVaultUri
output AZURE_STORAGE_ACCOUNT_NAME string = resources.outputs.storageAccountName
