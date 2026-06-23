param(
    [string]$Repo = "stephen-wu-chaohui/Planner",
    [string]$GitHubEnvironment = "dev",
    [string]$SubscriptionId,
    [string]$TenantId,
    [string]$ClientId,
    [string]$ResourceGroupName = "rg-planner-dev-aue",
    [string]$KeyVaultName
)

$ErrorActionPreference = "Stop"

foreach ($command in @("az", "gh")) {
    if (-not (Get-Command $command -ErrorAction SilentlyContinue)) {
        throw "Required command '$command' was not found on PATH."
    }
}

if (-not [string]::IsNullOrWhiteSpace($SubscriptionId)) {
    az account set --subscription $SubscriptionId
}

$account = az account show --query "{subscriptionId:id, tenantId:tenantId}" -o json | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($SubscriptionId)) {
    $SubscriptionId = $account.subscriptionId
}
if ([string]::IsNullOrWhiteSpace($TenantId)) {
    $TenantId = $account.tenantId
}
if ([string]::IsNullOrWhiteSpace($KeyVaultName)) {
    $KeyVaultName = az keyvault list --resource-group $ResourceGroupName --query "[0].name" -o tsv
}
if ([string]::IsNullOrWhiteSpace($ClientId)) {
    $ClientId = az keyvault secret show --vault-name $KeyVaultName --name github-deploy-client-id --query value -o tsv
}
if ([string]::IsNullOrWhiteSpace($ClientId) -or $ClientId -eq "pending-bootstrap") {
    throw "GitHub deploy client id was not found. Run bootstrap-entra.ps1 first."
}

$acrLoginServer = az acr list --resource-group $ResourceGroupName --query "[0].loginServer" -o tsv
$blazorAppName = az webapp list --resource-group $ResourceGroupName --query "[0].name" -o tsv
$containerAppsEnvironment = az containerapp env list --resource-group $ResourceGroupName --query "[0].name" -o tsv

$variables = [ordered]@{
    AZURE_RESOURCE_GROUP = $ResourceGroupName
    AZURE_CONTAINER_REGISTRY = $acrLoginServer
    AZURE_CONTAINERAPPS_ENVIRONMENT = $containerAppsEnvironment
    AZURE_BLAZOR_APP_NAME = $blazorAppName
    AZURE_API_CONTAINER_APP_NAME = "planner-dev-api"
    AZURE_OPTIMIZATION_CONTAINER_APP_NAME = "planner-dev-optimization-worker"
    AZURE_AI_WORKER_CONTAINER_APP_NAME = "planner-dev-ai-worker"
    AZURE_DB_MIGRATE_JOB_NAME = "planner-dev-db-migrate"
    AZURE_DB_SEED_JOB_NAME = "planner-dev-db-seed"
    AZURE_KEY_VAULT_NAME = $KeyVaultName
}

gh api -X PUT "repos/$Repo/environments/$GitHubEnvironment" 1>$null

gh secret set AZURE_CLIENT_ID --repo $Repo --env $GitHubEnvironment --body $ClientId
gh secret set AZURE_TENANT_ID --repo $Repo --env $GitHubEnvironment --body $TenantId
gh secret set AZURE_SUBSCRIPTION_ID --repo $Repo --env $GitHubEnvironment --body $SubscriptionId

foreach ($entry in $variables.GetEnumerator()) {
    if ([string]::IsNullOrWhiteSpace($entry.Value)) {
        throw "Value for GitHub variable '$($entry.Key)' is empty."
    }
    gh variable set $entry.Key --repo $Repo --env $GitHubEnvironment --body $entry.Value
}

Write-Host "GitHub environment '$GitHubEnvironment' configured for $Repo."
