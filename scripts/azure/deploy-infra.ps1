param(
    [string]$SubscriptionId,
    [string]$Location = "australiaeast",
    [string]$ResourceGroupName = "rg-planner-dev-aue",
    [string]$NamePrefix = "planner-dev",
    [string]$PlannerHostName = "planner.plannerdemo.com",
    [string]$ApiHostName = "api.plannerdemo.com",
    [string]$EntraDomain = "plannerdemo.com",
    [string]$SqlAdminLogin = "planneradmin",
    [string]$ApiImage,
    [string]$OptimizationWorkerImage,
    [string]$OptimizationJobWorkerImage,
    [string]$AiWorkerImage,
    [string]$DbMigratorImage,
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

function Require-Command {
    param([string]$Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found on PATH."
    }
}

function New-RandomPassword {
    param([int]$Length = 36)
    $alphabet = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789"
    -join (1..$Length | ForEach-Object { $alphabet[(Get-Random -Maximum $alphabet.Length)] })
}

function Get-ExistingKeyVaultName {
    param([string]$RgName)
    try {
        $name = az keyvault list --resource-group $RgName --query "[0].name" -o tsv 2>$null
        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($name)) {
            return $name.Trim()
        }
    } catch {
        return $null
    }
    return $null
}

function Get-KeyVaultSecretValue {
    param(
        [string]$VaultName,
        [string]$SecretName
    )
    if ([string]::IsNullOrWhiteSpace($VaultName)) {
        return $null
    }

    try {
        $value = az keyvault secret show --vault-name $VaultName --name $SecretName --query value -o tsv 2>$null
        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($value) -and $value -ne "pending-bootstrap") {
            return $value.Trim()
        }
    } catch {
        return $null
    }
    return $null
}

function Get-CurrentAzurePrincipalObjectId {
    $currentAccount = az account show -o json | ConvertFrom-Json
    if ($currentAccount.user.type -eq "user") {
        return [pscustomobject]@{
            ObjectId = (az ad signed-in-user show --query id -o tsv).Trim()
            PrincipalType = "User"
        }
    }

    if ($currentAccount.user.type -eq "servicePrincipal") {
        return [pscustomobject]@{
            ObjectId = (az ad sp show --id $currentAccount.user.name --query id -o tsv).Trim()
            PrincipalType = "ServicePrincipal"
        }
    }

    return $null
}

function Get-ExistingContainerAppImage {
    param(
        [string]$RgName,
        [string]$AppName
    )

    try {
        $image = az containerapp show --resource-group $RgName --name $AppName --query "properties.template.containers[0].image" -o tsv 2>$null
        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($image)) {
            return $image.Trim()
        }
    } catch {
        return $null
    }

    return $null
}

function Get-ExistingContainerAppJobImage {
    param(
        [string]$RgName,
        [string]$JobName
    )

    try {
        $image = az containerapp job show --resource-group $RgName --name $JobName --query "properties.template.containers[0].image" -o tsv 2>$null
        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($image)) {
            return $image.Trim()
        }
    } catch {
        return $null
    }

    return $null
}

Require-Command az

if (-not [string]::IsNullOrWhiteSpace($SubscriptionId)) {
    az account set --subscription $SubscriptionId
}

$account = az account show --query "{subscriptionId:id, tenantId:tenantId}" -o json | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($SubscriptionId)) {
    $SubscriptionId = $account.subscriptionId
}
$TenantId = $account.tenantId

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$templateFile = Join-Path $repoRoot "infra\main.bicep"

$existingKeyVaultName = Get-ExistingKeyVaultName -RgName $ResourceGroupName
$sqlAdminPassword = Get-KeyVaultSecretValue -VaultName $existingKeyVaultName -SecretName "sql-admin-password"
$rabbitMqPassword = Get-KeyVaultSecretValue -VaultName $existingKeyVaultName -SecretName "rabbitmq-pass"

if ([string]::IsNullOrWhiteSpace($sqlAdminPassword)) {
    $sqlAdminPassword = New-RandomPassword
    Write-Host "Generated a new SQL administrator password."
} else {
    Write-Host "Reusing SQL administrator password from Key Vault '$existingKeyVaultName'."
}

if ([string]::IsNullOrWhiteSpace($rabbitMqPassword)) {
    $rabbitMqPassword = New-RandomPassword
    Write-Host "Generated a new RabbitMQ password."
} else {
    Write-Host "Reusing RabbitMQ password from Key Vault '$existingKeyVaultName'."
}

if ([string]::IsNullOrWhiteSpace($ApiImage)) {
    $ApiImage = Get-ExistingContainerAppImage -RgName $ResourceGroupName -AppName "$NamePrefix-api"
}
if ([string]::IsNullOrWhiteSpace($OptimizationWorkerImage)) {
    $OptimizationWorkerImage = Get-ExistingContainerAppImage -RgName $ResourceGroupName -AppName "$NamePrefix-optimization-worker"
}
if ([string]::IsNullOrWhiteSpace($OptimizationJobWorkerImage)) {
    $OptimizationJobWorkerImage = Get-ExistingContainerAppJobImage -RgName $ResourceGroupName -JobName "$NamePrefix-optimization-job-worker"
}
if ([string]::IsNullOrWhiteSpace($AiWorkerImage)) {
    $AiWorkerImage = Get-ExistingContainerAppImage -RgName $ResourceGroupName -AppName "$NamePrefix-ai-worker"
}
if ([string]::IsNullOrWhiteSpace($DbMigratorImage)) {
    $DbMigratorImage = Get-ExistingContainerAppJobImage -RgName $ResourceGroupName -JobName "$NamePrefix-db-migrate"
}

$deploymentName = "planner-dev-" + (Get-Date -Format "yyyyMMddHHmmss")
$parameters = @(
    "location=$Location",
    "resourceGroupName=$ResourceGroupName",
    "namePrefix=$NamePrefix",
    "sqlAdminLogin=$SqlAdminLogin",
    "sqlAdminPassword=$sqlAdminPassword",
    "rabbitMqPassword=$rabbitMqPassword",
    "plannerHostName=$PlannerHostName",
    "apiHostName=$ApiHostName",
    "entraDomain=$EntraDomain",
    "entraTenantId=$TenantId"
)

if (-not [string]::IsNullOrWhiteSpace($ApiImage)) {
    Write-Host "Using API image: $ApiImage"
    $parameters += "apiImage=$ApiImage"
}
if (-not [string]::IsNullOrWhiteSpace($OptimizationWorkerImage)) {
    Write-Host "Using optimization worker image: $OptimizationWorkerImage"
    $parameters += "optimizationWorkerImage=$OptimizationWorkerImage"
}
if (-not [string]::IsNullOrWhiteSpace($OptimizationJobWorkerImage)) {
    Write-Host "Using optimization job worker image: $OptimizationJobWorkerImage"
    $parameters += "optimizationJobWorkerImage=$OptimizationJobWorkerImage"
}
if (-not [string]::IsNullOrWhiteSpace($AiWorkerImage)) {
    Write-Host "Using AI worker image: $AiWorkerImage"
    $parameters += "aiWorkerImage=$AiWorkerImage"
}
if (-not [string]::IsNullOrWhiteSpace($DbMigratorImage)) {
    Write-Host "Using DbMigrator image: $DbMigratorImage"
    $parameters += "dbMigratorImage=$DbMigratorImage"
}

if ($WhatIf) {
    az deployment sub what-if `
        --name $deploymentName `
        --location $Location `
        --template-file $templateFile `
        --parameters $parameters
    exit $LASTEXITCODE
}

$outputsJson = az deployment sub create `
    --name $deploymentName `
    --location $Location `
    --template-file $templateFile `
    --parameters $parameters `
    --query properties.outputs `
    -o json

if ($LASTEXITCODE -ne 0) {
    throw "Bicep deployment failed."
}

$outputsPath = Join-Path $repoRoot "infra\.last-deployment-outputs.json"
$outputsJson | Out-File -FilePath $outputsPath -Encoding utf8
$outputs = $outputsJson | ConvertFrom-Json
$keyVaultName = $outputs.keyVaultName.value

try {
    $principal = Get-CurrentAzurePrincipalObjectId
    if ($null -ne $principal -and -not [string]::IsNullOrWhiteSpace($principal.ObjectId) -and -not [string]::IsNullOrWhiteSpace($keyVaultName)) {
        $keyVaultId = az keyvault show --name $keyVaultName --query id -o tsv
        az role assignment create `
            --assignee-object-id $principal.ObjectId `
            --assignee-principal-type $principal.PrincipalType `
            --role "Key Vault Secrets Officer" `
            --scope $keyVaultId 1>$null 2>$null
        Write-Host "Granted Key Vault Secrets Officer to the current Azure principal for '$keyVaultName'."
    }
} catch {
    Write-Warning "Could not grant Key Vault Secrets Officer automatically. Grant it manually before running secret bootstrap scripts. $($_.Exception.Message)"
}

Write-Host "Deployment complete."
Write-Host "Outputs written to $outputsPath"
Write-Host ""
Write-Host "Next:"
Write-Host "  1. Run scripts/azure/bootstrap-entra.ps1"
Write-Host "  2. Run scripts/azure/set-runtime-secrets.ps1 with rotated Google/Firebase/Gemini/Maps values"
Write-Host "  3. Run scripts/azure/configure-github.ps1"
Write-Host "  4. Run scripts/azure/configure-custom-domains.ps1 and add DNS records"
