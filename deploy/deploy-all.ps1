# ==============================================================
#  Deploy-All.ps1 (Lightweight / Hardened)
#  Runs full Planner deployment with skip flags and safe output.
# ==============================================================

# Force UTF-8 output so emoji and icons render correctly
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

param(
    [switch]$SkipSharedPre,
    [switch]$SkipAPI,
    [switch]$SkipBlazor,
    [switch]$SkipWorker,
    [switch]$SkipSharedPost
)

# Optional: hide Azure CLI info/warning lines (keeps console clean)
az config set core.only_show_errors=True | Out-Null

$ErrorActionPreference = "Stop"

function Run-Step {
    param(
        [string]$Script,
        [string]$Description
    )

    Write-Host ""
    Write-Host "-----------------------------------------------"
    Write-Host "▶  $Description"
    Write-Host "-----------------------------------------------"

    try {
        # Capture all output (including warnings) but ignore harmless ones
        $output = & ".\$Script" *>&1
        if ($LASTEXITCODE -ne 0 -and ($output -match "ERROR|Error|Failed")) {
            throw "Step '$Description' failed. See log above."
        }
        Write-Host "✅ Completed: $Description"
    }
    catch {
        Write-Host "❌ Error during $Description" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "==============================================="
Write-Host "   🚀  Planner Lightweight Deployment Start"
Write-Host "==============================================="

if (-not $SkipSharedPre)  { Run-Step "deploy-shared-pre.ps1"  "Shared Environment Setup" }
if (-not $SkipAPI)        { Run-Step "deploy-api.ps1"         "Planner.API Build & Deploy" }
if (-not $SkipBlazor)     { Run-Step "deploy-blazor.ps1"      "Planner.BlazorApp Build & Deploy" }
if (-not $SkipWorker)     { Run-Step "deploy-worker.ps1"      "Planner.Worker Build & Deploy" }
if (-not $SkipSharedPost) { Run-Step "deploy-shared-post.ps1" "Shared Secrets Injection" }

Write-Host ""
Write-Host "==============================================="
Write-Host "✅  Planner Deployment Pipeline Finished"
Write-Host "==============================================="
Write-Host ""
Write-Host "To verify all Container Apps:"
Write-Host "  az containerapp list -g PlannerRG -o table"
Write-Host ""
Write-Host "To open Blazor app:"
Write-Host "  (az containerapp show -n planner-blazor -g PlannerRG --query `"properties.configuration.ingress.fqdn`" -o tsv) | ForEach-Object { Start-Process https://$_ }"
Write-Host ""
