param(
    [string]$ResourceGroupName = "rg-planner-dev-aue",
    [string]$KeyVaultName,
    [string]$FirestoreProjectId,
    [string]$FirebaseConfigJsonPath,
    [string]$FirebaseConfigJsonBase64,
    [string]$GoogleApiKey,
    [string]$GoogleMapsApiKey,
    [string]$GoogleMapsMapId,
    [string]$GeminiApiKey,
    [string]$GeminiModel = "gemini-2.5-flash"
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Required command 'az' was not found on PATH."
}

if ([string]::IsNullOrWhiteSpace($KeyVaultName)) {
    $KeyVaultName = az keyvault list --resource-group $ResourceGroupName --query "[0].name" -o tsv
}
if ([string]::IsNullOrWhiteSpace($KeyVaultName)) {
    throw "Key Vault name was not provided and could not be discovered in '$ResourceGroupName'."
}

if ([string]::IsNullOrWhiteSpace($FirebaseConfigJsonBase64) -and -not [string]::IsNullOrWhiteSpace($FirebaseConfigJsonPath)) {
    $rawJson = Get-Content -LiteralPath $FirebaseConfigJsonPath -Raw
    $FirebaseConfigJsonBase64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($rawJson))
}

$values = [ordered]@{
    "firestore-project-id" = $FirestoreProjectId
    "firebase-config-json" = $FirebaseConfigJsonBase64
    "google-api-key" = $GoogleApiKey
    "google-maps-api-key" = $GoogleMapsApiKey
    "google-maps-map-id" = $GoogleMapsMapId
    "gemini-api-key" = $GeminiApiKey
    "gemini-model" = $GeminiModel
}

foreach ($entry in $values.GetEnumerator()) {
    if ([string]::IsNullOrWhiteSpace($entry.Value)) {
        Write-Host "Skipping '$($entry.Key)' because no value was provided."
        continue
    }

    az keyvault secret set --vault-name $KeyVaultName --name $entry.Key --value $entry.Value --only-show-errors 1>$null
    Write-Host "Updated Key Vault secret '$($entry.Key)'."
}

Write-Host "Runtime secret update complete."
