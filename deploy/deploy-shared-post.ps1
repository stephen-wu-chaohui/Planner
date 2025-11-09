# ==============================================================
#  Deploy-Shared-Post.ps1
#  Refresh shared secrets and restart all Planner container apps
# ==============================================================

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"

# ----- Configuration -----
$RESOURCE_GROUP = "PlannerRG"
$ENV_NAME       = "planner-env"

# Update these secrets as needed
$RABBITMQ_HOST = "planner-rabbitmq.internal.agreeabletree-f7b70f39.australiaeast.azurecontainerapps.io"
$RABBITMQ_USER = "guest"
$RABBITMQ_PASS = "guest"
$RABBITMQ_PORT = "5672"
$SQLSERVER_CONNECTION = "Server=tcp:planner-sql.database.windows.net,1433;Initial Catalog=PlannerDB;User ID=planneruser;Password=StrongPassword123!"
$GOOGLE_MAPS_API_KEY = "AIzaSyAw5AmwZQuKsWoxwU5LhDD5YjVMahPk41A"

Write-Host "`n==============================================="
Write-Host " Refreshing Shared Secrets in Container App Env"
Write-Host "==============================================="

# ----- Apply shared secrets to environment -----
az containerapp env secrets set `
  --name $ENV_NAME `
  --resource-group $RESOURCE_GROUP `
  --secrets `
    rabbitmq-host=$RABBITMQ_HOST `
    rabbitmq-user=$RABBITMQ_USER `
    rabbitmq-pass=$RABBITMQ_PASS `
    rabbitmq-port=$RABBITMQ_PORT `
    sqlserver-connection=$SQLSERVER_CONNECTION `
    googlemaps-apikey=$GOOGLE_MAPS_API_KEY `
  | Out-Null

Write-Host "✅ Environment secrets updated."

# ----- Restart all Planner container apps -----
$apps = @("planner-api", "planner-blazor", "planner-worker")

foreach ($app in $apps) {
    Write-Host "`nRestarting $app ..."
    try {
        az containerapp revision restart -n $app -g $RESOURCE_GROUP | Out-Null
        Write-Host "   ✅ Restart signal sent to $app."
    }
    catch {
        Write-Host "   ⚠  Could not restart $app automatically. It may not exist yet."
    }
}

# ----- Verify status -----
Write-Host "`n==============================================="
Write-Host " Checking Container App Revisions"
Write-Host "==============================================="

az containerapp list -g $RESOURCE_GROUP -o table

Write-Host "`n✅ Shared secret refresh and restarts complete."
