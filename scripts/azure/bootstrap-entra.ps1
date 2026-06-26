param(
    [string]$SubscriptionId,
    [string]$ResourceGroupName = "rg-planner-dev-aue",
    [string]$KeyVaultName,
    [string]$Repo = "stephen-wu-chaohui/Planner",
    [string]$GitHubEnvironment = "dev",
    [string]$PlannerHostName = "planner.plannerdemo.com",
    [string]$ApiHostName = "api.plannerdemo.com",
    [string]$EntraDomain = "plannerdemo.com",
    [string]$ApiAppDisplayName = "planner-dev-api",
    [string]$BlazorAppDisplayName = "planner-dev-blazor",
    [string]$GitHubDeployAppDisplayName = "planner-dev-github-deploy",
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$DemoPassword
)

$ErrorActionPreference = "Stop"

function Require-Command {
    param([string]$Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found on PATH."
    }
}

function Get-AppByDisplayName {
    param([string]$DisplayName)
    $json = az ad app list --display-name $DisplayName --query "[0]" -o json
    if ([string]::IsNullOrWhiteSpace($json) -or $json -eq "null") {
        return $null
    }
    return $json | ConvertFrom-Json
}

function Ensure-App {
    param(
        [string]$DisplayName,
        [string]$SignInAudience = "AzureADMyOrg"
    )
    $app = Get-AppByDisplayName -DisplayName $DisplayName
    if ($null -eq $app) {
        Write-Host "Creating app registration '$DisplayName'."
        $app = az ad app create --display-name $DisplayName --sign-in-audience $SignInAudience -o json | ConvertFrom-Json
    } else {
        Write-Host "Using existing app registration '$DisplayName'."
    }

    az ad sp create --id $app.appId 1>$null 2>$null
    return Get-AppByDisplayName -DisplayName $DisplayName
}

function Set-KeyVaultSecret {
    param(
        [string]$Name,
        [string]$Value
    )
    if ([string]::IsNullOrWhiteSpace($KeyVaultName)) {
        return
    }
    az keyvault secret set --vault-name $KeyVaultName --name $Name --value $Value --only-show-errors 1>$null
}

function Set-GraphApplication {
    param(
        [string]$ObjectId,
        [hashtable]$Body
    )
    $json = $Body | ConvertTo-Json -Depth 20 -Compress
    $tempFile = New-TemporaryFile
    try {
        $json | Out-File -FilePath $tempFile -Encoding utf8
        az rest --method PATCH --uri "https://graph.microsoft.com/v1.0/applications/$ObjectId" --headers "Content-Type=application/json" --body "@$tempFile" 1>$null
    } finally {
        Remove-Item -LiteralPath $tempFile -Force -ErrorAction SilentlyContinue
    }
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

if ([string]::IsNullOrWhiteSpace($KeyVaultName)) {
    $KeyVaultName = az keyvault list --resource-group $ResourceGroupName --query "[0].name" -o tsv
}
if ([string]::IsNullOrWhiteSpace($KeyVaultName)) {
    throw "Key Vault name was not provided and could not be discovered in '$ResourceGroupName'."
}

$apiApp = Ensure-App -DisplayName $ApiAppDisplayName
$apiIdentifierUri = "api://$($apiApp.appId)"
$existingApiScope = $apiApp.api.oauth2PermissionScopes | Where-Object { $_.value -eq "API.Access" } | Select-Object -First 1
$apiScopeId = if ($existingApiScope -and $existingApiScope.id) { $existingApiScope.id } else { [guid]::NewGuid().ToString() }

Set-GraphApplication -ObjectId $apiApp.id -Body @{
    identifierUris = @($apiIdentifierUri)
    api = @{
        requestedAccessTokenVersion = 2
        oauth2PermissionScopes = @(
            @{
                id = $apiScopeId
                value = "API.Access"
                type = "User"
                isEnabled = $true
                adminConsentDisplayName = "Access Planner API"
                adminConsentDescription = "Allow Planner clients to access Planner.API."
                userConsentDisplayName = "Access Planner API"
                userConsentDescription = "Allow Planner to call Planner.API on your behalf."
            }
        )
    }
}
$apiApp = Get-AppByDisplayName -DisplayName $ApiAppDisplayName

$blazorApp = Ensure-App -DisplayName $BlazorAppDisplayName
Set-GraphApplication -ObjectId $blazorApp.id -Body @{
    spa = @{
        redirectUris = @(
            "https://$PlannerHostName/authentication/login-callback",
            "https://localhost:7014/authentication/login-callback",
            "http://localhost:5212/authentication/login-callback"
        )
    }
    isFallbackPublicClient = $true
    requiredResourceAccess = @(
        @{
            resourceAppId = $apiApp.appId
            resourceAccess = @(
                @{
                    id = $apiScopeId
                    type = "Scope"
                }
            )
        }
    )
}
az ad app permission admin-consent --id $blazorApp.appId 1>$null
$blazorApp = Get-AppByDisplayName -DisplayName $BlazorAppDisplayName

$githubApp = Ensure-App -DisplayName $GitHubDeployAppDisplayName
$subject = "repo:${Repo}:environment:${GitHubEnvironment}"
$existingFederatedCredential = az ad app federated-credential list --id $githubApp.appId --query "[?name=='github-$GitHubEnvironment'] | [0]" -o json
if ([string]::IsNullOrWhiteSpace($existingFederatedCredential) -or $existingFederatedCredential -eq "null") {
    $credential = @{
        name = "github-$GitHubEnvironment"
        issuer = "https://token.actions.githubusercontent.com"
        subject = $subject
        audiences = @("api://AzureADTokenExchange")
        description = "GitHub Actions OIDC for $Repo environment $GitHubEnvironment"
    } | ConvertTo-Json -Depth 10

    $credentialFile = New-TemporaryFile
    try {
        $credential | Out-File -FilePath $credentialFile -Encoding utf8
        az ad app federated-credential create --id $githubApp.appId --parameters "@$credentialFile" 1>$null
    } finally {
        Remove-Item -LiteralPath $credentialFile -Force -ErrorAction SilentlyContinue
    }
}

$githubSpObjectId = az ad sp show --id $githubApp.appId --query id -o tsv
$resourceGroupId = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupName"
az role assignment create --assignee-object-id $githubSpObjectId --assignee-principal-type ServicePrincipal --role Contributor --scope $resourceGroupId 1>$null 2>$null

$acrId = az acr list --resource-group $ResourceGroupName --query "[0].id" -o tsv
if (-not [string]::IsNullOrWhiteSpace($acrId)) {
    az role assignment create --assignee-object-id $githubSpObjectId --assignee-principal-type ServicePrincipal --role AcrPush --scope $acrId 1>$null 2>$null
}

Set-KeyVaultSecret -Name "azuread-tenant-id" -Value $TenantId
Set-KeyVaultSecret -Name "azuread-domain" -Value $EntraDomain
Set-KeyVaultSecret -Name "azuread-api-client-id" -Value $apiApp.appId
Set-KeyVaultSecret -Name "azuread-blazor-client-id" -Value $blazorApp.appId
Set-KeyVaultSecret -Name "api-scope" -Value "$apiIdentifierUri/API.Access"
Set-KeyVaultSecret -Name "github-deploy-client-id" -Value $githubApp.appId

$cities = @("taipei", "perth", "sydney", "melbourne", "auckland", "christchurch")
foreach ($city in $cities) {
    $upn = "$city.admin@$EntraDomain"
    $exists = $true
    az ad user show --id $upn 1>$null 2>$null
    if ($LASTEXITCODE -ne 0) {
        $exists = $false
    }

    if ($exists) {
        Write-Host "User exists: $upn"
        continue
    }

    Write-Host "Creating demo user: $upn"
    az ad user create `
        --display-name "$city Planner Admin" `
        --user-principal-name $upn `
        --mail-nickname "$city.admin" `
        --password $DemoPassword `
        --force-change-password-next-sign-in false `
        --account-enabled true 1>$null
}

Write-Host "Entra bootstrap complete."
Write-Host "GitHub deploy client id: $($githubApp.appId)"
Write-Host "API client id: $($apiApp.appId)"
Write-Host "Blazor client id: $($blazorApp.appId)"
