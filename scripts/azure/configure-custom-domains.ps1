param(
    [string]$ResourceGroupName = "rg-planner-dev-aue",
    [string]$PlannerHostName = "planner.plannerdemo.com",
    [string]$ApiHostName = "api.plannerdemo.com",
    [switch]$Bind
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Required command 'az' was not found on PATH."
}

$webAppName = az webapp list --resource-group $ResourceGroupName --query "[0].name" -o tsv
$webDefaultHostName = az webapp show --resource-group $ResourceGroupName --name $webAppName --query defaultHostName -o tsv
$webVerificationId = az webapp show --resource-group $ResourceGroupName --name $webAppName --query customDomainVerificationId -o tsv

$apiAppName = "planner-dev-api"
$apiDefaultHostName = az containerapp show --resource-group $ResourceGroupName --name $apiAppName --query properties.configuration.ingress.fqdn -o tsv
$apiVerificationId = az containerapp show --resource-group $ResourceGroupName --name $apiAppName --query properties.customDomainVerificationId -o tsv

Write-Host "Create these DNS records with the existing DNS provider:"
Write-Host ""
Write-Host "Blazor:"
Write-Host "  CNAME planner -> $webDefaultHostName"
Write-Host "  TXT   asuid.planner -> $webVerificationId"
Write-Host ""
Write-Host "API:"
Write-Host "  CNAME api -> $apiDefaultHostName"
Write-Host "  TXT   asuid.api -> $apiVerificationId"
Write-Host ""

if (-not $Bind) {
    Write-Host "After DNS is visible, rerun this script with -Bind."
    exit 0
}

Write-Host "Binding App Service custom domain and managed certificate..."
az webapp config hostname add --resource-group $ResourceGroupName --webapp-name $webAppName --hostname $PlannerHostName 1>$null
$certThumbprint = az webapp config ssl create --resource-group $ResourceGroupName --name $webAppName --hostname $PlannerHostName --query thumbprint -o tsv
az webapp config ssl bind --resource-group $ResourceGroupName --name $webAppName --certificate-thumbprint $certThumbprint --ssl-type SNI 1>$null

Write-Host "Binding Container Apps custom domain and managed certificate..."
az containerapp hostname add --resource-group $ResourceGroupName --name $apiAppName --hostname $ApiHostName 1>$null
az containerapp hostname bind --resource-group $ResourceGroupName --name $apiAppName --hostname $ApiHostName --validation-method CNAME 1>$null

Write-Host "Custom domain binding complete."
