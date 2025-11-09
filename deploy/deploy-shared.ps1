# ==============================================================
# Deploy Shared Environment and Secrets for Planner
# ==============================================================

# Configuration
$RESOURCE_GROUP = "planner-rg"
$ACR_NAME       = "plannerregistry"
$ENV_NAME       = "planner-env"
$LOCATION       = "australiaeast"

# 1. Login and ensure environment exists
Write-Host "Logging in to Azure..."
az account show 1>$null 2>$null
if ($LASTEXITCODE -ne 0) {
    az login --only-show-errors | Out-Null
}

Write-Host "Ensuring resource group and environment exist..."
az group create --name $RESOURCE_GROUP --location $LOCATION --output none
az containerapp env show -n $ENV_NAME -g $RESOURCE_GROUP 1>$null 2>$null
if ($LASTEXITCODE -ne 0) {
    az containerapp env create --name $ENV_NAME --resource-group $RESOURCE_GROUP --location $LOCATION --output none
}

# 2. Shared secret values
$rabbitmqHost        = "planner-rabbitmq.internal.agreeabletree-f7b70f39.australiaeast.azurecontainerapps.io"
$rabbitmqUser        = "guest"
$rabbitmqPass        = "guest"
$rabbitmqPort        = "5672"
$sqlserverConnection = "Server=tcp:planner-sql.database.windows.net,1433;Initial Catalog=PlannerDb;User ID=sqladmin;Password=<your-password>;Encrypt=True;"
$googlemapsApikey    = "AIzaSyAw5AmwZQuKsWoxwU5LhDD5YjVMahPk41A"

# 3. Ensure planner-api exists before applying secrets
$apiExists = az containerapp show -n planner-api -g $RESOURCE_GROUP --query "name" -o tsv 2>$null
if (-not $apiExists) {
    Write-Host "Planner.API not found. Creating placeholder..."
    az containerapp create `
        --name planner-api `
        --resource-group $RESOURCE_GROUP `
        --environment $ENV_NAME `
        --image "mcr.microsoft.com/dotnet/aspnet:8.0" `
        --target-port 8080 `
        --ingress internal | Out-Null
}

# 4. Apply shared secrets
Write-Host "Creating or updating shared secrets for planner-api..."
az containerapp secret set `
    --name planner-api `
    --resource-group $RESOURCE_GROUP `
    --secrets `
    rabbitmq-host="$rabbitmqHost" `
    rabbitmq-user="$rabbitmqUser" `
    rabbitmq-pass="$rabbitmqPass" `
    rabbitmq-port="$rabbitmqPort" `
    sqlserver-connection="$sqlserverConnection" `
    googlemaps-apikey="$googlemapsApikey"

# 5. Confirm results
Write-Host "Listing stored secret names..."
az containerapp secret list --name planner-api --resource-group $RESOURCE_GROUP --query "[].name" -o tsv

Write-Host ""
Write-Host "Shared environment [$ENV_NAME] and secrets are ready."
Write-Host "Next steps:"
Write-Host "  1. Run  deploy-api.ps1"
Write-Host "  2. Run  deploy-blazor.ps1"
Write-Host "  3. Run  deploy-worker.ps1"
Write-Host ""
