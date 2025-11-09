# ==============================================================
#  check-status.ps1
#  Quick diagnostics for all Planner Container Apps
# ==============================================================

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"

$RESOURCE_GROUP = "PlannerRG"
$APPS = @("planner-api", "planner-blazor", "planner-worker")

Write-Host "`n==============================================="
Write-Host "   Checking Planner Container Apps Status"
Write-Host "==============================================="

# ----- Environment summary -----
Write-Host "`n🔍 Listing Container Apps in $RESOURCE_GROUP ..."
az containerapp list -g $RESOURCE_GROUP -o table

# ----- Show ingress URLs -----
Write-Host "`n🌐 Ingress URLs:"
foreach ($app in $APPS) {
    try {
        $fqdn = az containerapp show -n $app -g $RESOURCE_GROUP --query "properties.configuration.ingress.fqdn" -o tsv
        if ($fqdn) {
            Write-Host "  $app → https://$fqdn"
        } else {
            Write-Host "  $app → (no external ingress)"
        }
    } catch {
        Write-Host "  ⚠  Unable to query ingress for $app"
    }
}

# ----- Show running revisions -----
Write-Host "`n⚙️  Active revisions:"
foreach ($app in $APPS) {
    try {
        az containerapp revision list -n $app -g $RESOURCE_GROUP --query "[].{App:name,Revision:name,Active:properties.active,Created:properties.createdTime}" -o table
    } catch {
        Write-Host "  ⚠  No revisions found for $app"
    }
}

# ----- Show recent logs -----
Write-Host "`n📜 Recent log samples:"
foreach ($app in $APPS) {
    Write-Host "`n--- $app ---"
    try {
        az containerapp logs show -n $app -g $RESOURCE_GROUP --tail 15
    } catch {
        Write-Host "  ⚠  Logs unavailable for $app (may not have started yet)"
    }
}

Write-Host "`n==============================================="
Write-Host "✅ Diagnostics complete. Review results above."
Write-Host "==============================================="
