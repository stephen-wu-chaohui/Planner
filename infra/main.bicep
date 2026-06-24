targetScope = 'subscription'

@description('Azure region for all Planner dev resources.')
param location string = 'australiaeast'

@description('Resource group name for the Planner dev deployment.')
param resourceGroupName string = 'rg-planner-dev-aue'

@description('Name prefix used for Azure resources.')
param namePrefix string = 'planner-dev'

@description('Short suffix for globally unique Azure resource names. Defaults to a deterministic value for the subscription.')
param uniqueSuffix string = toLower(take(uniqueString(subscription().subscriptionId, namePrefix, location), 6))

@description('SQL administrator login name.')
param sqlAdminLogin string = 'planneradmin'

@secure()
@description('SQL administrator password. The deployment script generates or reuses this from Key Vault.')
param sqlAdminPassword string

@secure()
@description('RabbitMQ password. The deployment script generates or reuses this from Key Vault.')
param rabbitMqPassword string

@description('RabbitMQ username.')
param rabbitMqUser string = 'planner'

@description('Custom hostname for the Blazor app.')
param plannerHostName string = 'planner.plannerdemo.com'

@description('Custom hostname for the API.')
param apiHostName string = 'api.plannerdemo.com'

@description('Entra ID domain used by Planner app registrations.')
param entraDomain string = 'plannerdemo.com'

@description('Entra ID tenant id. Defaults to the tenant used for the deployment.')
param entraTenantId string = tenant().tenantId

@description('Container image for the Planner API Container App.')
param apiImage string = 'mcr.microsoft.com/dotnet/samples:aspnetapp'

@description('Container image for the optimization worker Container App.')
param optimizationWorkerImage string = 'mcr.microsoft.com/dotnet/samples:aspnetapp'

@description('Container image for the async optimization Container Apps job worker.')
param optimizationJobWorkerImage string = 'mcr.microsoft.com/dotnet/samples:dotnetapp'

@description('Container image for the AI worker Container App.')
param aiWorkerImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

@description('Container image for the DbMigrator Container Apps jobs.')
param dbMigratorImage string = 'mcr.microsoft.com/dotnet/aspnet:10.0'

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
  tags: {
    app: 'Planner'
    environment: 'dev'
  }
}

module core 'modules/core.bicep' = {
  name: 'planner-dev-core'
  scope: rg
  params: {
    location: location
    namePrefix: namePrefix
    uniqueSuffix: uniqueSuffix
    sqlAdminLogin: sqlAdminLogin
    sqlAdminPassword: sqlAdminPassword
    rabbitMqUser: rabbitMqUser
    rabbitMqPassword: rabbitMqPassword
    plannerHostName: plannerHostName
    apiHostName: apiHostName
    entraDomain: entraDomain
    entraTenantId: entraTenantId
    apiImage: apiImage
    optimizationWorkerImage: optimizationWorkerImage
    optimizationJobWorkerImage: optimizationJobWorkerImage
    aiWorkerImage: aiWorkerImage
    dbMigratorImage: dbMigratorImage
  }
}

output resourceGroupName string = rg.name
output azureRegion string = location
output names object = core.outputs.names
output githubVariables object = core.outputs.githubVariables
output dnsRecords object = core.outputs.dnsRecords
output keyVaultName string = core.outputs.keyVaultName
