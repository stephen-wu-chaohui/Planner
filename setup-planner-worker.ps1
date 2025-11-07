# ============================================================
# 🧱 Planner.Optimization.Worker bootstrap script
# Works with Azure CLI 2.78+
# ============================================================

param(
    [string]$ResourceGroup   = "PlannerRG",
    [string]$Location        = "australiaeast",
    [string]$AcrName         = "plannerregistry",
    [string]$EnvName         = "planner-env",
    [string]$AppName         = "planner-optimization-worker",
    [string]$KeyVaultName    = "planner-kv-sw",
    [string]$AppConfigUrl    = "https://planner-appconfig.azconfig.io"
)

Write-Host "🔎 Checking Azure login..."
if (-not (az account show 2>$null)) {
    Write-Host "Please run 'az login' first." -ForegroundColor Yellow
    exit 1
}

Write-Host "🔎 Ensuring resource group..."
if (-not (az group show -n $ResourceGroup --query name -o tsv 2>$null)) {
    az group create -n $ResourceGroup -l $Location | Out-Null
}

# ---------- Log Analytics Workspace ----------
$workspace = "planner-logs"
if (-not (az monitor log-analytics workspace show -g $ResourceGroup -n $workspace --query id -o tsv 2>$null)) {
    Write-Host "🧠 Creating Log Analytics workspace..."
    az monitor log-analytics workspace create -g $ResourceGroup -n $workspace -l $Location | Out-Null
}
$logId  = az monitor log-analytics workspace show -g $ResourceGroup -n $workspace --query customerId -o tsv
$logKey = az monitor log-analytics workspace get-shared-keys -g $ResourceGroup -n $workspace --query primarySharedKey -o tsv

# ---------- Container App Environment ----------
if (-not (az containerapp env show -n $EnvName -g $ResourceGroup --query id -o tsv 2>$null)) {
    Write-Host "🏗️  Creating Container App environment..."
    az containerapp env create `
        -n $EnvName `
        -g $ResourceGroup `
        -l $Location `
        --logs-workspace-id  $logId `
        --logs-workspace-key $logKey | Out-Null
}

# ---------- Azure Container Registry ----------
if (-not (az acr show -n $AcrName -g $ResourceGroup --query id -o tsv 2>$null)) {
    Write-Host "🐳 Creating ACR..."
    az acr create -n $AcrName -g $ResourceGroup -l $Location --sku Basic --admin-enabled true | Out-Null
}
$acrLoginServer = az acr show -n $AcrName --query loginServer -o tsv
$image = "$acrLoginServer/$AppName:latest"

# ---------- Container App (create / update) ----------
if (az containerapp show -n $AppName -g $ResourceGroup --query id -o tsv 2>$null) {
    Write-Host "🔄 Updating existing Container App..."
    az containerapp update `
        -n $AppName `
        -g $ResourceGroup `
        --image $image `
        --set-env-vars `
            "AppConfig__Endpoint=$AppConfigUrl" `
            "AzureServicesAuthConnectionString=RunAs=ManagedIdentity" | Out-Null
}
else {
    Write-Host "🚀 Creating new Container App..."
    az containerapp create `
        -n $AppName `
        -g $ResourceGroup `
        --environment $EnvName `
        --image $image `
        --cpu 1 --memory 2Gi `
        --min-replicas 1 --max-replicas 1 `
        --assign-identity system `
        --env-vars `
            "AppConfig__Endpoint=$AppConfigUrl" `
            "AzureServicesAuthConnectionString=RunAs=ManagedIdentity" | Out-Null
}

# ---------- Key Vault RBAC ----------
$principalId = az containerapp show -g $ResourceGroup -n $AppName --query identity.principalId -o tsv
$vaultId     = az keyvault show -n $KeyVaultName -g $ResourceGroup --query id -o tsv

if ($principalId -and $vaultId) {
    Write-Host "🔐 Granting Key Vault read access..."
    az role assignment create `
        --role "Key Vault Secrets User" `
        --assignee-object-id $principalId `
        --assignee-principal-type ServicePrincipal `
        --scope $vaultId `
        --only-show-errors | Out-Null
}
else {
    Write-Host "⚠️  Could not retrieve principalId or vaultId." -ForegroundColor Yellow
}

# ---------- Summary ----------
Write-Host "✅ $AppName container app is ready!" -ForegroundColor Green
Write-Host "   Managed Identity: $principalId"
Write-Host "   Key Vault:        $KeyVaultName"
Write-Host "   App Config:       $AppConfigUrl"
Write-Host "   Image:            $image""
