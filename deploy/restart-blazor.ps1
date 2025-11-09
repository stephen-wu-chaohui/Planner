# ===============================================
# Restart planner-blazor Container App
# Compatible with containerapp v1.2.0b5
# ===============================================

param (
    [string]$RESOURCE_GROUP = "PlannerRG",
    [string]$APP_NAME       = "planner-blazor"
)

Write-Host "==============================================="
Write-Host " Restarting $($APP_NAME)"
Write-Host "==============================================="

# Get latest revision
$latestRevision = az containerapp revision list `
    -n "$($APP_NAME)" `
    -g "$($RESOURCE_GROUP)" `
    --query "[?active==\`true\`].name" `
    -o tsv

if (-not $latestRevision) {
    Write-Host "No active revision found for $($APP_NAME)." -ForegroundColor Red
    exit 1
}

Write-Host "Latest active revision: $($latestRevision)"

# Deactivate and reactivate revision (simulates restart)
Write-Host "Deactivating revision..."
az containerapp revision deactivate `
    -n "$($APP_NAME)" `
    -g "$($RESOURCE_GROUP)" `
    --revision "$($latestRevision)" `
    | Out-Null

Start-Sleep -Seconds 10

Write-Host "Reactivating revision..."
az containerapp revision activate `
    -n "$($APP_NAME)" `
    -g "$($RESOURCE_GROUP)" `
    --revision "$($latestRevision)" `
    | Out-Null

Start-Sleep -Seconds 10

Write-Host ""
Write-Host "Checking replica status..."
az containerapp revision list `
    -n "$($APP_NAME)" `
    -g "$($RESOURCE_GROUP)" `
    -o table

Write-Host ""
Write-Host "✅ Restart completed for $($APP_NAME)"
Write-Host "==============================================="
