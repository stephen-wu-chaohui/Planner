# ===============================================
# Deploy Planner.BlazorApp (always tag :latest)
# Compatible with containerapp v1.2.0b5
# ===============================================

param (
    [string]$RESOURCE_GROUP = "PlannerRG",
    [string]$ENV_NAME       = "planner-env",
    [string]$APP_FOLDER     = "Planner.BlazorApp",
    [string]$ACR_NAME       = "plannerregistry"
)

Write-Host "==============================================="
Write-Host " Deploying $($APP_FOLDER)  (tag = latest)"
Write-Host "==============================================="

# Normalize image repo name (lowercase + replace dots)
$IMAGE_REPO = $APP_FOLDER.ToLower() -replace '\.', '-'
$APP_NAME   = "planner-blazor"

# ----- paths -----
$SRC_PATH   = "C:\projects\planner\src"
$DOCKERFILE = "$($SRC_PATH)\$($APP_FOLDER)\Dockerfile"
$IMAGE_TAG  = "$($ACR_NAME).azurecr.io/$($IMAGE_REPO):latest"

Write-Host ""
Write-Host "SRC_PATH   = $SRC_PATH"
Write-Host "DOCKERFILE = $DOCKERFILE"
Write-Host "IMAGE_TAG  = $IMAGE_TAG"
Write-Host ""

# ----- build & push -----
Write-Host "Building and pushing $($IMAGE_REPO) image to $($ACR_NAME) ..."
az acr build `
  --registry "$($ACR_NAME)" `
  --image "$($IMAGE_REPO):latest" `
  --file "$($DOCKERFILE)" `
  "$($SRC_PATH)"

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Build failed.  Aborting deployment." -ForegroundColor Red
    exit 1
}

# ----- update container app -----
Write-Host ""
Write-Host "Updating container app '$($APP_NAME)' to use latest image ..."
az containerapp update `
  -n "$($APP_NAME)" `
  -g "$($RESOURCE_GROUP)" `
  --image "$($IMAGE_TAG)" `
  | Out-Null

# ----- verify -----
Write-Host ""
Write-Host "Waiting 30 seconds for revision to activate ..."
Start-Sleep -Seconds 30

Write-Host ""
Write-Host "Current revisions:"
az containerapp revision list -n "$($APP_NAME)" -g "$($RESOURCE_GROUP)" -o table

Write-Host ""
Write-Host "✅ $($APP_NAME) deployed successfully (tag = latest)"
Write-Host "==============================================="
