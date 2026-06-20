targetScope = 'resourceGroup'

param location string
param sqlLocation string
param deploySql bool
param namePrefix string
param suffix string
param tags object
param entraTenantId string
param entraClientId string
param allowedOrigin string
param appServicePlanSku string

// ── Name variables ────────────────────────────────────────────────────────────
var apiServiceName           = 'api'
var appName                  = '${namePrefix}-api-${suffix}'
var effectiveOrigin          = empty(allowedOrigin) ? 'https://${appName}.azurewebsites.net' : allowedOrigin
var sqlServerName            = '${namePrefix}-sql-${suffix}'
var kvName                   = take('kv-${replace(namePrefix, '-', '')}${suffix}', 24)
var storageAccountName       = take(toLower(replace('${namePrefix}${suffix}', '-', '')), 24)
var vnetName                 = '${namePrefix}-vnet-${suffix}'

// Well-known Azure RBAC role definition IDs
var signalRAppServerRoleId      = '420fcaa2-552c-430f-98ca-3264be4806c7'
var kvSecretsUserRoleId         = '4633458b-17de-408a-b874-0445c86b69e6'
var storageBlobContribRoleId    = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'

// ── Log Analytics ─────────────────────────────────────────────────────────────
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${namePrefix}-logs-${suffix}'
  location: location
  tags: tags
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

// ── Application Insights ──────────────────────────────────────────────────────
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${namePrefix}-appi-${suffix}'
  location: location
  kind: 'web'
  tags: tags
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    DisableIpMasking: false
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// ── VNet 10.1.0.0/16 ─────────────────────────────────────────────────────────
//   snet-app 10.1.1.0/24 – App Service VNet Integration (delegation)
//   snet-pe  10.1.2.0/24 – Private Endpoints
resource vnet 'Microsoft.Network/virtualNetworks@2023-11-01' = {
  name: vnetName
  location: location
  tags: tags
  properties: {
    addressSpace: { addressPrefixes: ['10.1.0.0/16'] }
    subnets: [
      {
        name: 'snet-app'
        properties: {
          addressPrefix: '10.1.1.0/24'
          delegations: [
            {
              name: 'app-service-delegation'
              properties: { serviceName: 'Microsoft.Web/serverFarms' }
            }
          ]
          privateEndpointNetworkPolicies: 'Enabled'
          privateLinkServiceNetworkPolicies: 'Enabled'
        }
      }
      {
        name: 'snet-pe'
        properties: {
          addressPrefix: '10.1.2.0/24'
          privateEndpointNetworkPolicies: 'Disabled'
          privateLinkServiceNetworkPolicies: 'Enabled'
        }
      }
    ]
  }
}

var appSubnetId = '${vnet.id}/subnets/snet-app'
var peSubnetId  = '${vnet.id}/subnets/snet-pe'

// ── Private DNS Zones (global) ────────────────────────────────────────────────
resource dnsSql 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  // Zone name is Azure-required; disabling URL lint rule
  #disable-next-line no-hardcoded-env-urls
  name: 'privatelink.database.windows.net'
  location: 'global'
  tags: tags
}
resource dnsKv 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.vaultcore.azure.net'
  location: 'global'
  tags: tags
}
resource dnsBlob 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  // Zone name is Azure-required; disabling URL lint rule
  #disable-next-line no-hardcoded-env-urls
  name: 'privatelink.blob.core.windows.net'
  location: 'global'
  tags: tags
}

// VNet links so App Service DNS resolves PE IPs
resource vnetLinkSql 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: dnsSql
  name: 'link-sql'
  location: 'global'
  tags: tags
  properties: { virtualNetwork: { id: vnet.id }, registrationEnabled: false }
}
resource vnetLinkKv 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: dnsKv
  name: 'link-kv'
  location: 'global'
  tags: tags
  properties: { virtualNetwork: { id: vnet.id }, registrationEnabled: false }
}
resource vnetLinkBlob 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: dnsBlob
  name: 'link-blob'
  location: 'global'
  tags: tags
  properties: { virtualNetwork: { id: vnet.id }, registrationEnabled: false }
}

// ── SignalR Free F1 ───────────────────────────────────────────────────────────
resource signalR 'Microsoft.SignalRService/signalR@2024-03-01' = {
  name: '${namePrefix}-signalr-${suffix}'
  location: location
  tags: tags
  sku: { name: 'Free_F1', tier: 'Free', capacity: 1 }
  kind: 'SignalR'
  properties: {
    features: [
      { flag: 'ServiceMode',            value: 'Default' }
      { flag: 'EnableConnectivityLogs', value: 'True'    }
      { flag: 'EnableMessagingLogs',    value: 'False'   }
    ]
    cors: { allowedOrigins: [effectiveOrigin] }
    publicNetworkAccess: 'Enabled'
    tls: { clientCertEnabled: false }
  }
}

// ── App Service Plan (B2 Linux) ───────────────────────────────────────────────
resource appServicePlan 'Microsoft.Web/serverfarms@2024-11-01' = {
  name: '${namePrefix}-plan-${suffix}'
  location: location
  kind: 'linux'
  tags: tags
  sku: { name: appServicePlanSku }
  properties: { reserved: true }
}

// ── App Service – VNet Integration via virtualNetworkSubnetId ─────────────────
resource webApp 'Microsoft.Web/sites@2024-11-01' = {
  name: appName
  location: location
  kind: 'app,linux'
  tags: union(tags, { 'azd-service-name': apiServiceName })
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    clientAffinityEnabled: false
    publicNetworkAccess: 'Enabled'         // POC: keep on for pipeline deploys
    virtualNetworkSubnetId: appSubnetId    // Regional VNet Integration → snet-app
    siteConfig: {
      alwaysOn: true
      ftpsState: 'Disabled'
      healthCheckPath: '/health'
      http20Enabled: true
      linuxFxVersion: 'DOTNETCORE|10.0'
      minTlsVersion: '1.2'
      scmMinTlsVersion: '1.2'
      use32BitWorkerProcess: false
      webSocketsEnabled: true
      vnetRouteAllEnabled: true            // Route all outbound traffic through VNet
      cors: {
        allowedOrigins: [effectiveOrigin]
        supportCredentials: true
      }
    }
  }
}

// ── Azure SQL (Serverless GP_S_Gen5) ──────────────────────────────────────────
// publicNetworkAccess: Enabled – POC allows pipeline access; traffic from App
// Service always uses the Private Endpoint (DNS resolves to 10.1.2.x via VNet).
resource sqlServer 'Microsoft.Sql/servers@2023-08-01' = if (deploySql) {
  name: sqlServerName
  location: sqlLocation   // GP_S_Gen5 may be restricted in the main region
  tags: tags
  properties: {
    // SQL auth is disabled below; this placeholder login can never be used.
    administratorLogin: 'sqladmin-placeholder'
    // SQL auth is disabled by sqlAadOnlyAuth below; this password is never usable.
    #disable-next-line use-secure-value-for-secure-inputs
    administratorLoginPassword: '${uniqueString(resourceGroup().id, sqlServerName)}Spotly-1!'
    publicNetworkAccess: 'Enabled'
    minimalTlsVersion: '1.2'
    version: '12.0'
  }
}

// Set App Service Managed Identity as AAD admin
resource sqlAadAdmin 'Microsoft.Sql/servers/administrators@2023-08-01' = if (deploySql) {
  parent: sqlServer
  name: 'ActiveDirectory'
  properties: {
    administratorType: 'ActiveDirectory'
    login: webApp.name
    sid: webApp.identity.principalId
    tenantId: entraTenantId
  }
}

// Disable SQL authentication – MI only (must be after AAD admin is set)
resource sqlAadOnlyAuth 'Microsoft.Sql/servers/azureADOnlyAuthentications@2023-08-01' = if (deploySql) {
  parent: sqlServer
  name: 'Default'
  properties: { azureADOnlyAuthentication: true }
  dependsOn: [sqlAadAdmin]
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01' = if (deploySql) {
  parent: sqlServer
  name: 'SpotlyDB'
  location: sqlLocation
  tags: tags
  sku: {
    name: 'GP_S_Gen5_1'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 1
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
  // Serverless mode is specified by GP_S_Gen5_1 SKU (the _S_ prefix)
    autoPauseDelay: 60
    minCapacity: json('0.5')
    zoneRedundant: false
    requestedBackupStorageRedundancy: 'Local'
  }
}

// ── Key Vault Standard (RBAC) ─────────────────────────────────────────────────
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  // kvName is always ≥ 3 chars ('kv-' prefix); BCP334 is a false positive
  #disable-next-line BCP334
  name: kvName
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: { family: 'A', name: 'standard' }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    publicNetworkAccess: 'Enabled'         // POC: keep on for pipeline access
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

// ── Storage Account LRS (floor-plans) ────────────────────────────────────────
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  // storageAccountName always ≥ 3 chars; BCP334 is a false positive
  #disable-next-line BCP334
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  tags: tags
  sku: { name: 'Standard_LRS' }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false            // Managed Identity only
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'Enabled'         // POC: keep on for pipeline access
    networkAcls: { defaultAction: 'Allow', bypass: 'AzureServices' }
    supportsHttpsTrafficOnly: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

// Single container; paths floor-plans/parking/ and floor-plans/desks/ are prefixes
resource floorPlansContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'floor-plans'
  properties: { publicAccess: 'None' }
}

// ── Private Endpoints (snet-pe) ───────────────────────────────────────────────
resource peSql 'Microsoft.Network/privateEndpoints@2023-11-01' = if (deploySql) {
  name: '${namePrefix}-pe-sql-${suffix}'
  location: location
  tags: tags
  properties: {
    subnet: { id: peSubnetId }
    privateLinkServiceConnections: [
      {
        name: 'sql-plsc'
        properties: {
          privateLinkServiceId: sqlServer.id
          groupIds: ['sqlServer']
        }
      }
    ]
  }
}

resource peKv 'Microsoft.Network/privateEndpoints@2023-11-01' = {
  name: '${namePrefix}-pe-kv-${suffix}'
  location: location
  tags: tags
  properties: {
    subnet: { id: peSubnetId }
    privateLinkServiceConnections: [
      {
        name: 'kv-plsc'
        properties: {
          privateLinkServiceId: keyVault.id
          groupIds: ['vault']
        }
      }
    ]
  }
}

resource peStorage 'Microsoft.Network/privateEndpoints@2023-11-01' = {
  name: '${namePrefix}-pe-storage-${suffix}'
  location: location
  tags: tags
  properties: {
    subnet: { id: peSubnetId }
    privateLinkServiceConnections: [
      {
        name: 'storage-plsc'
        properties: {
          privateLinkServiceId: storageAccount.id
          groupIds: ['blob']
        }
      }
    ]
  }
}

// DNS Zone Groups – auto-populate private DNS when PE is provisioned
resource peSqlDnsGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-11-01' = if (deploySql) {
  parent: peSql
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'privatelink-database-windows-net'
        properties: { privateDnsZoneId: dnsSql.id }
      }
    ]
  }
}

resource peKvDnsGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-11-01' = {
  parent: peKv
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'privatelink-vaultcore-azure-net'
        properties: { privateDnsZoneId: dnsKv.id }
      }
    ]
  }
}

resource peStorageDnsGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-11-01' = {
  parent: peStorage
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'privatelink-blob-core-windows-net'
        properties: { privateDnsZoneId: dnsBlob.id }
      }
    ]
  }
}

// ── App Settings ──────────────────────────────────────────────────────────────
resource appSettings 'Microsoft.Web/sites/config@2024-11-01' = {
  parent: webApp
  name: 'appsettings'
  properties: {
    ASPNETCORE_ENVIRONMENT: 'Production'
    APPLICATIONINSIGHTS_CONNECTION_STRING: applicationInsights.properties.ConnectionString
    ApplicationInsightsAgent_EXTENSION_VERSION: '~3'
    // SignalR: MSI auth – no connection-string secret needed
    Azure__SignalR__ConnectionString: 'Endpoint=https://${signalR.properties.hostName};AuthType=azure.msi;Version=1.0;'
    // SQL: MSI auth via Active Directory Default (empty when deploySql=false)
    Azure__Sql__ConnectionString: deploySql ? 'Server=tcp:${any(sqlServer).properties.fullyQualifiedDomainName},1433;Database=SpotlyDB;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;' : ''
    // Key Vault URI for secret references
    Azure__KeyVault__Uri: keyVault.properties.vaultUri
    // Storage: MSI auth – only account name needed
    Azure__Storage__AccountName: storageAccount.name
    Azure__Storage__ContainerName: 'floor-plans'
    Booking__CheckInCutoffUtc: '09:30'
    Cors__AllowedOrigins__0: effectiveOrigin
    SCM_DO_BUILD_DURING_DEPLOYMENT: 'true'
  }
}

// ── Role Assignments ──────────────────────────────────────────────────────────
// SignalR App Server → SignalR resource
resource signalRAppServerRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(signalR.id, webApp.id, signalRAppServerRoleId)
  scope: signalR
  properties: {
    principalId: webApp.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', signalRAppServerRoleId)
  }
}

// Key Vault Secrets User → Key Vault
resource kvSecretsUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, webApp.id, kvSecretsUserRoleId)
  scope: keyVault
  properties: {
    principalId: webApp.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', kvSecretsUserRoleId)
  }
}

// Storage Blob Data Contributor → Storage Account
resource storageBlobContribRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, webApp.id, storageBlobContribRoleId)
  scope: storageAccount
  properties: {
    principalId: webApp.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobContribRoleId)
  }
}

// ── Auth Settings (Entra ID Easy Auth) ────────────────────────────────────────
resource authSettings 'Microsoft.Web/sites/config@2024-11-01' = {
  parent: webApp
  name: 'authsettingsV2'
  properties: {
    platform: {
      enabled: true
      runtimeVersion: '~1'
    }
    globalValidation: {
      requireAuthentication: true
      unauthenticatedClientAction: 'Return401'
      redirectToProvider: 'azureactivedirectory'
    }
    identityProviders: {
      azureActiveDirectory: {
        enabled: true
        registration: {
          clientId: entraClientId
          openIdIssuer: '${environment().authentication.loginEndpoint}${entraTenantId}/v2.0'
        }
        validation: {
          allowedAudiences: [
            'api://${entraClientId}'
            entraClientId
          ]
        }
      }
    }
    login: {
      tokenStore: { enabled: true }
    }
    httpSettings: {
      requireHttps: true
      routes: { apiPrefix: '/.auth' }
    }
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
output apiName string = webApp.name
output apiUri string = 'https://${webApp.properties.defaultHostName}'
output applicationInsightsConnectionString string = applicationInsights.properties.ConnectionString
output sqlServerFqdn string = deploySql ? any(sqlServer).properties.fullyQualifiedDomainName : ''
output keyVaultUri string = keyVault.properties.vaultUri
output storageAccountName string = storageAccount.name
