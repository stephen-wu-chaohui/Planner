param location string
param namePrefix string
param uniqueSuffix string
param sqlAdminLogin string
@secure()
param sqlAdminPassword string
param rabbitMqUser string
@secure()
param rabbitMqPassword string
param plannerHostName string
param apiHostName string
param entraDomain string
param entraTenantId string
param apiImage string = 'mcr.microsoft.com/dotnet/samples:aspnetapp'
param optimizationWorkerImage string = 'mcr.microsoft.com/dotnet/samples:aspnetapp'
param aiWorkerImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
param dbMigratorImage string = 'mcr.microsoft.com/dotnet/aspnet:10.0'

var compactPrefix = toLower(replace(namePrefix, '-', ''))
var suffix = toLower(uniqueSuffix)

var acrName = toLower(take('${compactPrefix}${suffix}acr', 50))
var appInsightsName = '${namePrefix}-${suffix}-appi'
var containerAppsEnvironmentName = '${namePrefix}-${suffix}-cae'
var keyVaultName = toLower(take('${namePrefix}-${suffix}-kv', 24))
var logAnalyticsName = '${namePrefix}-${suffix}-log'
var storageName = toLower(take('${compactPrefix}${suffix}st', 24))
var fileShareName = 'rabbitmq'
var appServicePlanName = '${namePrefix}-${suffix}-asp'
var blazorAppName = '${namePrefix}-${suffix}-web'
var sqlServerName = toLower('${namePrefix}-${suffix}-sql')
var sqlDatabaseName = 'PlannerDB-dev'

var apiContainerAppName = '${namePrefix}-api'
var optimizationWorkerContainerAppName = '${namePrefix}-optimization-worker'
var aiWorkerContainerAppName = '${namePrefix}-ai-worker'
var rabbitMqContainerAppName = 'rabbitmq'
var migrateJobName = '${namePrefix}-db-migrate'
var seedJobName = '${namePrefix}-db-seed'

var apiIdentityName = '${namePrefix}-api-id'
var optimizationWorkerIdentityName = '${namePrefix}-optimization-worker-id'
var aiWorkerIdentityName = '${namePrefix}-ai-worker-id'
var migratorIdentityName = '${namePrefix}-db-migrator-id'

var acrPullRoleId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

var plannerDbConnectionString = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabaseName};Persist Security Info=False;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    tenantId: entraTenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    publicNetworkAccess: 'Enabled'
    sku: {
      family: 'A'
      name: 'standard'
    }
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource fileService 'Microsoft.Storage/storageAccounts/fileServices@2023-05-01' = {
  parent: storage
  name: 'default'
}

resource rabbitMqShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2023-05-01' = {
  parent: fileService
  name: fileShareName
  properties: {
    shareQuota: 5
  }
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    publicNetworkAccess: 'Enabled'
    minimalTlsVersion: '1.2'
  }
}

resource sqlAllowAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
    capacity: 5
  }
  properties: {
    maxSizeBytes: 2147483648
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
    size: 'B1'
    capacity: 1
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource apiIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: apiIdentityName
  location: location
}

resource optimizationWorkerIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: optimizationWorkerIdentityName
  location: location
}

resource aiWorkerIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: aiWorkerIdentityName
  location: location
}

resource migratorIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: migratorIdentityName
  location: location
}

resource apiAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, apiIdentity.id, acrPullRoleId)
  scope: acr
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRoleId)
    principalId: apiIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource optimizationWorkerAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, optimizationWorkerIdentity.id, acrPullRoleId)
  scope: acr
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRoleId)
    principalId: optimizationWorkerIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource aiWorkerAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, aiWorkerIdentity.id, acrPullRoleId)
  scope: acr
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRoleId)
    principalId: aiWorkerIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource migratorAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, migratorIdentity.id, acrPullRoleId)
  scope: acr
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRoleId)
    principalId: migratorIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource apiKeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, apiIdentity.id, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: apiIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource optimizationWorkerKeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, optimizationWorkerIdentity.id, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: optimizationWorkerIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource aiWorkerKeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, aiWorkerIdentity.id, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: aiWorkerIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource migratorKeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, migratorIdentity.id, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: migratorIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: containerAppsEnvironmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

resource containerAppsStorage 'Microsoft.App/managedEnvironments/storages@2024-03-01' = {
  parent: containerAppsEnvironment
  name: 'rabbitmq-data'
  properties: {
    azureFile: {
      accountName: storage.name
      accountKey: storage.listKeys().keys[0].value
      shareName: rabbitMqShare.name
      accessMode: 'ReadWrite'
    }
  }
}

resource plannerDbConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'plannerdb-connection'
  properties: {
    value: plannerDbConnectionString
  }
}

resource sqlAdminPasswordSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'sql-admin-password'
  properties: {
    value: sqlAdminPassword
  }
}

resource rabbitMqUserSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'rabbitmq-user'
  properties: {
    value: rabbitMqUser
  }
}

resource rabbitMqPasswordSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'rabbitmq-pass'
  properties: {
    value: rabbitMqPassword
  }
}

resource firestoreProjectIdSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'firestore-project-id'
  properties: {
    value: 'pending-bootstrap'
  }
}

resource firebaseConfigJsonSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'firebase-config-json'
  properties: {
    value: 'pending-bootstrap'
  }
}

resource googleApiKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'google-api-key'
  properties: {
    value: 'pending-bootstrap'
  }
}

resource googleMapsApiKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'google-maps-api-key'
  properties: {
    value: 'pending-bootstrap'
  }
}

resource googleMapsMapIdSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'google-maps-map-id'
  properties: {
    value: 'pending-bootstrap'
  }
}

resource geminiApiKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'gemini-api-key'
  properties: {
    value: 'pending-bootstrap'
  }
}

resource geminiModelSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'gemini-model'
  properties: {
    value: 'gemini-2.5-flash'
  }
}

resource azureAdTenantIdSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'azuread-tenant-id'
  properties: {
    value: entraTenantId
  }
}

resource azureAdDomainSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'azuread-domain'
  properties: {
    value: entraDomain
  }
}

resource azureAdApiClientIdSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'azuread-api-client-id'
  properties: {
    value: 'pending-bootstrap'
  }
}

resource azureAdBlazorClientIdSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'azuread-blazor-client-id'
  properties: {
    value: 'pending-bootstrap'
  }
}

resource azureAdBlazorClientSecretSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'azuread-blazor-client-secret'
  properties: {
    value: 'pending-bootstrap'
  }
}

resource apiScopeSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'api-scope'
  properties: {
    value: 'api://pending-bootstrap/API.Access'
  }
}

resource githubDeployClientIdSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'github-deploy-client-id'
  properties: {
    value: 'pending-bootstrap'
  }
}

var keyVaultSecretDependencies = [
  plannerDbConnectionSecret
  sqlAdminPasswordSecret
  rabbitMqUserSecret
  rabbitMqPasswordSecret
  firestoreProjectIdSecret
  firebaseConfigJsonSecret
  googleApiKeySecret
  googleMapsApiKeySecret
  googleMapsMapIdSecret
  geminiApiKeySecret
  geminiModelSecret
  azureAdTenantIdSecret
  azureAdDomainSecret
  azureAdApiClientIdSecret
  azureAdBlazorClientIdSecret
  azureAdBlazorClientSecretSecret
  apiScopeSecret
  githubDeployClientIdSecret
]

resource blazorApp 'Microsoft.Web/sites@2023-12-01' = {
  name: blazorAppName
  location: location
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    clientAffinityEnabled: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      webSocketsEnabled: true
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'Api__BaseUrl'
          value: 'https://${apiHostName}'
        }
        {
          name: 'Api__Scope'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/api-scope)'
        }
        {
          name: 'AzureAd__Instance'
          value: 'https://login.microsoftonline.com/'
        }
        {
          name: 'AzureAd__Domain'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/azuread-domain)'
        }
        {
          name: 'AzureAd__TenantId'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/azuread-tenant-id)'
        }
        {
          name: 'AzureAd__ClientId'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/azuread-blazor-client-id)'
        }
        {
          name: 'AzureAd__ClientSecret'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/azuread-blazor-client-secret)'
        }
        {
          name: 'AzureAd__Scopes'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/api-scope)'
        }
        {
          name: 'AzureAd__CallbackPath'
          value: '/signin-oidc'
        }
        {
          name: 'GoogleMaps__ApiKey'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/google-maps-api-key)'
        }
        {
          name: 'GoogleMaps__MapId'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/google-maps-map-id)'
        }
        {
          name: 'Firestore__ProjectId'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/firestore-project-id)'
        }
        {
          name: 'FIREBASE_CONFIG_JSON'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/firebase-config-json)'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
      ]
    }
  }
  dependsOn: keyVaultSecretDependencies
}

resource blazorKeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, blazorApp.id, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: blazorApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource rabbitMqApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: rabbitMqContainerAppName
  location: location
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 5672
        transport: 'tcp'
        exposedPort: 5672
      }
      secrets: [
        {
          name: 'rabbitmq-user'
          value: rabbitMqUser
        }
        {
          name: 'rabbitmq-pass'
          value: rabbitMqPassword
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'rabbitmq'
          image: 'rabbitmq:3-management'
          env: [
            {
              name: 'RABBITMQ_DEFAULT_USER'
              secretRef: 'rabbitmq-user'
            }
            {
              name: 'RABBITMQ_DEFAULT_PASS'
              secretRef: 'rabbitmq-pass'
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          volumeMounts: [
            {
              volumeName: 'rabbitmq-data'
              mountPath: '/var/lib/rabbitmq/mnesia'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
      volumes: [
        {
          name: 'rabbitmq-data'
          storageType: 'AzureFile'
          storageName: containerAppsStorage.name
        }
      ]
    }
  }
  dependsOn: [
    containerAppsStorage
  ]
}

resource apiApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: apiContainerAppName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${apiIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
      }
      registries: [
        {
          server: acr.properties.loginServer
          identity: apiIdentity.id
        }
      ]
      secrets: [
        {
          name: 'plannerdb-connection'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/plannerdb-connection'
          identity: apiIdentity.id
        }
        {
          name: 'rabbitmq-user'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/rabbitmq-user'
          identity: apiIdentity.id
        }
        {
          name: 'rabbitmq-pass'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/rabbitmq-pass'
          identity: apiIdentity.id
        }
        {
          name: 'firestore-project-id'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/firestore-project-id'
          identity: apiIdentity.id
        }
        {
          name: 'firebase-config-json'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/firebase-config-json'
          identity: apiIdentity.id
        }
        {
          name: 'google-api-key'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/google-api-key'
          identity: apiIdentity.id
        }
        {
          name: 'gemini-api-key'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/gemini-api-key'
          identity: apiIdentity.id
        }
        {
          name: 'gemini-model'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/gemini-model'
          identity: apiIdentity.id
        }
        {
          name: 'azuread-domain'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/azuread-domain'
          identity: apiIdentity.id
        }
        {
          name: 'azuread-tenant-id'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/azuread-tenant-id'
          identity: apiIdentity.id
        }
        {
          name: 'azuread-api-client-id'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/azuread-api-client-id'
          identity: apiIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'planner-api'
          image: apiImage
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8080'
            }
            {
              name: 'ConnectionStrings__PlannerDb'
              secretRef: 'plannerdb-connection'
            }
            {
              name: 'RabbitMq__Host'
              value: rabbitMqApp.name
            }
            {
              name: 'RabbitMq__Port'
              value: '5672'
            }
            {
              name: 'RabbitMq__User'
              secretRef: 'rabbitmq-user'
            }
            {
              name: 'RabbitMq__Pass'
              secretRef: 'rabbitmq-pass'
            }
            {
              name: 'Firestore__ProjectId'
              secretRef: 'firestore-project-id'
            }
            {
              name: 'FIREBASE_CONFIG_JSON'
              secretRef: 'firebase-config-json'
            }
            {
              name: 'GOOGLE_API_KEY'
              secretRef: 'google-api-key'
            }
            {
              name: 'GEMINI_API_KEY'
              secretRef: 'gemini-api-key'
            }
            {
              name: 'GEMINI_MODEL'
              secretRef: 'gemini-model'
            }
            {
              name: 'AzureAd__Instance'
              value: 'https://login.microsoftonline.com/'
            }
            {
              name: 'AzureAd__Domain'
              secretRef: 'azuread-domain'
            }
            {
              name: 'AzureAd__TenantId'
              secretRef: 'azuread-tenant-id'
            }
            {
              name: 'AzureAd__ClientId'
              secretRef: 'azuread-api-client-id'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: appInsights.properties.ConnectionString
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
  dependsOn: [
    apiAcrPull
    apiKeyVaultSecretsUser
    rabbitMqApp
  ]
}

resource optimizationWorkerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: optimizationWorkerContainerAppName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${optimizationWorkerIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      registries: [
        {
          server: acr.properties.loginServer
          identity: optimizationWorkerIdentity.id
        }
      ]
      secrets: [
        {
          name: 'rabbitmq-user'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/rabbitmq-user'
          identity: optimizationWorkerIdentity.id
        }
        {
          name: 'rabbitmq-pass'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/rabbitmq-pass'
          identity: optimizationWorkerIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'planner-optimization-worker'
          image: optimizationWorkerImage
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'RabbitMq__Host'
              value: rabbitMqApp.name
            }
            {
              name: 'RabbitMq__Port'
              value: '5672'
            }
            {
              name: 'RabbitMq__User'
              secretRef: 'rabbitmq-user'
            }
            {
              name: 'RabbitMq__Pass'
              secretRef: 'rabbitmq-pass'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: appInsights.properties.ConnectionString
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
  dependsOn: [
    optimizationWorkerAcrPull
    optimizationWorkerKeyVaultSecretsUser
    rabbitMqApp
  ]
}

resource aiWorkerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: aiWorkerContainerAppName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${aiWorkerIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      registries: [
        {
          server: acr.properties.loginServer
          identity: aiWorkerIdentity.id
        }
      ]
      secrets: [
        {
          name: 'firebase-config-json'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/firebase-config-json'
          identity: aiWorkerIdentity.id
        }
        {
          name: 'gemini-api-key'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/gemini-api-key'
          identity: aiWorkerIdentity.id
        }
        {
          name: 'gemini-model'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/gemini-model'
          identity: aiWorkerIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'planner-ai-worker'
          image: aiWorkerImage
          env: [
            {
              name: 'PYTHONUNBUFFERED'
              value: '1'
            }
            {
              name: 'FIREBASE_CONFIG_JSON'
              secretRef: 'firebase-config-json'
            }
            {
              name: 'GEMINI_API_KEY'
              secretRef: 'gemini-api-key'
            }
            {
              name: 'GEMINI_MODEL'
              secretRef: 'gemini-model'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: appInsights.properties.ConnectionString
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
  dependsOn: [
    aiWorkerAcrPull
    aiWorkerKeyVaultSecretsUser
  ]
}

var migratorSecrets = [
  {
    name: 'plannerdb-connection'
    keyVaultUrl: '${keyVault.properties.vaultUri}secrets/plannerdb-connection'
    identity: migratorIdentity.id
  }
]

var migratorEnv = [
  {
    name: 'ConnectionStrings__PlannerDb'
    secretRef: 'plannerdb-connection'
  }
]

resource migrateJob 'Microsoft.App/jobs@2024-03-01' = {
  name: migrateJobName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${migratorIdentity.id}': {}
    }
  }
  properties: {
    environmentId: containerAppsEnvironment.id
    configuration: {
      triggerType: 'Manual'
      replicaTimeout: 1800
      replicaRetryLimit: 1
      manualTriggerConfig: {
        parallelism: 1
        replicaCompletionCount: 1
      }
      registries: [
        {
          server: acr.properties.loginServer
          identity: migratorIdentity.id
        }
      ]
      secrets: migratorSecrets
    }
    template: {
      containers: [
        {
          name: 'planner-db-migrator'
          image: dbMigratorImage
          command: [
            'dotnet'
            'Planner.Tools.DbMigrator.dll'
          ]
          args: [
            'migrate'
          ]
          env: migratorEnv
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
    }
  }
  dependsOn: [
    migratorAcrPull
    migratorKeyVaultSecretsUser
  ]
}

resource seedJob 'Microsoft.App/jobs@2024-03-01' = {
  name: seedJobName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${migratorIdentity.id}': {}
    }
  }
  properties: {
    environmentId: containerAppsEnvironment.id
    configuration: {
      triggerType: 'Manual'
      replicaTimeout: 1800
      replicaRetryLimit: 1
      manualTriggerConfig: {
        parallelism: 1
        replicaCompletionCount: 1
      }
      registries: [
        {
          server: acr.properties.loginServer
          identity: migratorIdentity.id
        }
      ]
      secrets: migratorSecrets
    }
    template: {
      containers: [
        {
          name: 'planner-db-migrator'
          image: dbMigratorImage
          command: [
            'dotnet'
            'Planner.Tools.DbMigrator.dll'
          ]
          args: [
            'seed'
          ]
          env: migratorEnv
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
    }
  }
  dependsOn: [
    migratorAcrPull
    migratorKeyVaultSecretsUser
    migrateJob
  ]
}

output keyVaultName string = keyVault.name
output names object = {
  acrLoginServer: acr.properties.loginServer
  acrName: acr.name
  apiContainerAppName: apiApp.name
  aiWorkerContainerAppName: aiWorkerApp.name
  blazorAppName: blazorApp.name
  containerAppsEnvironmentName: containerAppsEnvironment.name
  keyVaultName: keyVault.name
  migrateJobName: migrateJob.name
  optimizationWorkerContainerAppName: optimizationWorkerApp.name
  resourceGroupLocation: location
  seedJobName: seedJob.name
  sqlDatabaseName: sqlDatabase.name
  sqlServerName: sqlServer.name
}
output githubVariables object = {
  AZURE_API_CONTAINER_APP_NAME: apiApp.name
  AZURE_AI_WORKER_CONTAINER_APP_NAME: aiWorkerApp.name
  AZURE_BLAZOR_APP_NAME: blazorApp.name
  AZURE_CONTAINER_REGISTRY: acr.properties.loginServer
  AZURE_CONTAINERAPPS_ENVIRONMENT: containerAppsEnvironment.name
  AZURE_DB_MIGRATE_JOB_NAME: migrateJob.name
  AZURE_DB_SEED_JOB_NAME: seedJob.name
  AZURE_KEY_VAULT_NAME: keyVault.name
  AZURE_OPTIMIZATION_CONTAINER_APP_NAME: optimizationWorkerApp.name
  AZURE_RESOURCE_GROUP: resourceGroup().name
}
output dnsRecords object = {
  planner: {
    host: plannerHostName
    cnameTarget: blazorApp.properties.defaultHostName
    txtName: 'asuid.${first(split(plannerHostName, '.'))}'
    txtValue: blazorApp.properties.customDomainVerificationId
  }
  api: {
    host: apiHostName
    cnameTarget: apiApp.properties.configuration.ingress.fqdn
    txtName: 'asuid.${first(split(apiHostName, '.'))}'
    txtValue: any(apiApp).properties.customDomainVerificationId
  }
}
