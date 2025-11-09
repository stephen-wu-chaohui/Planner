# ===============================================
#  Restart Planner.Worker (ContainerApp v1.2.0b5)
# ===============================================

param (
    [string]$RESOURCE_GROUP = "PlannerRG",
    [string]$APP_NAME = "planner-worker",
    [string]$ACR_NAME = "plannerregistry"
)

Write-Host "==============================================="
Write-Host " Restarting $APP_NAME in $RESOURCE_GROUP"
Write-Host "==============================================="

# 1️⃣  Update environment variables
Write-Host "`nUpdating environment variables ..."
az containerapp update `
  -n $APP_NAME `
  -g $RESOURCE_GROUP `
  --set-env-vars `
    RABBITMQ__HOST="planner-rabbitmq.internal.agreeabletree-f7b70f39.australiaeast.azurecontainerapps.io" `
    RABBITMQ__USER="guest" `
    RABBITMQ__PASS="guest" `
    RABBITMQ__PORT="5672" `
    ASPNETCORE_URLS="http://+:8080" `
    ASPNETCORE_ENVIRONMENT="Production" `
  | Out-Null

# 2️⃣  Force a new revision (restart trick)
Write-Host "`nForcing new revision with latest image ..."
$imageTag = "$ACR_NAME.azurecr.io/$APP_NAME:latest"
az containerapp update `
  -n $APP_NAME `
  -g $RESOURCE_GROUP `
  --image $imageTag `
  | Out-Null

# 3️⃣  Wait & verify health
Write-Host "`nWaiting for new revision to stabilize ..."
Start-Sleep -Seconds 30

Write-Host "`nCurrent revisions:"
az containerapp revision list -n $APP_NAME -g $RESOURCE_GROUP -o table

Write-Host "`nLatest logs:"
az containerapp logs show -n $APP_NAME -g $RESOURCE_GROUP --tail 40

Write-Host "`n✅ Restart sequence complete."
Write-Host "==============================================="
