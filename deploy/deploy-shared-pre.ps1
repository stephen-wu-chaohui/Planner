# ==============================================================
#  deploy-shared-pre.ps1
#  Ensures shared Azure resources and identity exist
# ==============================================================

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"

$RESOURCE_GROUP = "PlannerRG"
$REGISTRY_NAME  = "plannerregistry"
$ENV_NAME       = "planner-env"
$IDENTITY_NAME  = "planner-env-identity"
$LOCATION       = "australiaeast"

Write-Host "`n==============================================="
Write-Host " Preparing shared environment for Planner"
Write-Host "==============================================="

# ----- Ensure Container Apps Environment exists -----
$envExists = az containerapp env list -g $RESOURCE_GROUP --query "[?name=='$ENV_NAME'] | length(@)" -o tsv
if ($envExists -eq 0) {
    Write-Host "Creating container app environment: $ENV_NAME ..."
    az containerapp env create `
        --name $ENV_NAME `
        --resource-group $RESOURCE_GROUP `
        --location $LOCATION `
        --infrastructure-subnet-resource-id "" `
        --logs-destination none | Out-Null
} else {
    Write-Host "✅ Container App Environment already exists: $ENV_NAME"
}

# ----- Ensure Managed Identity exists -----
$idExists = az identity list -g $RESOURCE_GROUP --query "[?name=='$IDENTITY_NAME'] | length(@)" -o tsv
if ($idExists -eq 0) {
    Write-Host "Creating managed identity: $IDENTITY_NAME ..."
    az identity create --name $IDENTITY_NAME --resource-group $RESOURCE_GROUP --location $LOCATION | Out-Null
} else {
    Write-Host "✅ Managed identity already exists: $IDENTITY_NAME"
}

# ----- Ensure ACR Pull Role assignment -----
$IDENTITY_PRINCIPAL_ID = az identity show -n $IDENTITY_NAME -g $RESOURCE_GROUP --query "principalId" -o tsv
$ACR_ID = az acr show -n $REGISTRY_NAME --query "id" -o tsv

$roleExists = az role assignment list `
    --assignee $IDENTITY_PRINCIPAL_ID `
    --role "AcrPull" `
    --scope $ACR_ID `
    --query "length(@)" -o tsv

if ($roleExists -eq 0) {
    Write-Host "Granting AcrPull role to $IDENTITY_NAME for $REGISTRY_NAME ..."
    az role assignment create `
        --assignee $IDENTITY_PRINCIPAL_ID `
        --role "AcrPull" `
        --scope $ACR_ID | Out-Null
} else {
    Write-Host "✅ AcrPull permission already granted to $IDENTITY_NAME"
}

Write-Host "`n✅ Shared pre-deployment setup complete."
