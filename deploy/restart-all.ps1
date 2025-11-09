# ==============================================================
# Restart all Planner Container Apps (interactive menu)
# ==============================================================

$RESOURCE_GROUP = "PlannerRG"
$apps = @("planner-api","planner-blazor","planner-worker")

Write-Host ""
Write-Host "==============================================="
Write-Host "  Planner Container App Restart Utility"
Write-Host "==============================================="
Write-Host ""
Write-Host "1) Quick restart (restart current revision)"
Write-Host "2) Roll restart (force new revision)"
Write-Host ""
$choice = Read-Host "Select restart method (1 or 2)"

switch ($choice) {

    "1" {
        Write-Host ""
        Write-Host "Performing QUICK restart for all apps..."
        foreach ($app in $apps) {
            Write-Host "Restarting $app ..."
            az containerapp revision restart `
                --name $app `
                --resource-group $RESOURCE_GROUP | Out-Null
        }
        Write-Host ""
        Write-Host "✅ Quick restart completed for all Container Apps."
    }

    "2" {
        Write-Host ""
        Write-Host "Performing ROLL restart (new revisions)..."
        $timestamp = Get-Date -Format o
        foreach ($app in $apps) {
            Write-Host "Rolling $app to a new revision..."
            az containerapp update `
                --name $app `
                --resource-group $RESOURCE_GROUP `
                --set-env-vars FORCE_RESTART="$timestamp" | Out-Null
        }
        Write-Host ""
        Write-Host "✅ Roll restart completed. New revisions will start shortly."
    }

    Default {
        Write-Host "Invalid selection. Please choose 1 or 2."
    }
}

Write-Host ""
Write-Host "Tip: You can monitor startup logs with:"
Write-Host "  az containerapp logs show -n planner-api -g $RESOURCE_GROUP --follow"
Write-Host ""
