# ==============================================================
# Deploy Planner.API (framework-dependent .NET 8 ASP.NET Core)
# ==============================================================

$RESOURCE_GROUP = "PlannerRG"
$ENV_NAME       = "planner-env"
$ACR_NAME       = "plannerregistry"
$APP_NAME       = "planner-api"
$LOCATION       = "australiaeast"
$SRC_PATH       = "..\src"
$DOCKERFILE     = "$SRC_PATH\Planner.API\Dockerfile"

# Full image tag (used consistently in ACR and Container App)
$IMAGE_TAG      = "$ACR_NAME.azurecr.io/$APP_NAME:latest"

Write-Host ""
Write-Host "==============================================="
Write-Host " Deploying $APP_NAME to Azure Container Apps"
Write-Host "==============================================="

# --------------------------------------------------------------
# 1️⃣  Build & Push Docker image to ACR
# --------------------------------------------------------------
Write-Host ""
Write-Host "Building and pushing $APP_NAME image to $ACR_NAME ..."

# Regenerate Dockerfile to ensure clean ASCII content and proper runtime
@"
# -------------------- Dockerfile (auto-regenerated) --------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Planner.API/Planner.API.csproj", "Planner.API/"]
RUN dotnet restore "Planner.API/Planner.API.csproj"
COPY . .
WORKDIR "/src/Planner.API"
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Planner.API.dll"]
# -----------------------------------------------------------------------
"@ | Set-Content -Path $DOCKERFILE -Encoding ascii

# Run ACR build
az acr build `
  --registry $ACR_NAME `
  --image $IMAGE_TAG `
  --file $DOCKERFILE `
  $SRC_PATH | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed. Check ACR build logs in Azure Portal." -ForegroundColor Red
    exit 1
}

Write-Host "✅ Docker image for $APP_NAME built and pushed successfully."

# --------------------------------------------------------------
# 2️⃣  Create or Update Container App
# --------------------------------------------------------------
$exists = az containerapp show -n $APP_NAME -g $RESOURCE_GROUP --query "name" -o tsv 2>$null

if (-not $exists) {
    Write-Host "Creating new Container App: $APP_NAME ..."
    az containerapp create `
        --name $APP_NAME `
        --resource-group $RESOURCE_GROUP `
        --environment $ENV_NAME `
        --image $IMAGE_TAG `
        --ingress external `
        --target-port 8080 `
        --min-replicas 1 `
        --max-replicas 2 `
        --cpu 0.5 `
        --memory 1.0Gi `
        --query "properties.configuration.ingress.fqdn" | Out-Null
}
else {
    Write-Host "Updating existing Container App: $APP_NAME ..."
    az containerapp update `
        --name $APP_NAME `
        --resource-group $RESOURCE_GROUP `
        --image $IMAGE_TAG | Out-Null
}

Write-Host ""
Write-Host "✅ $APP_NAME deployed successfully."
Write-Host ""
Write-Host "To verify image and ingress:"
Write-Host "  az containerapp show -n $APP_NAME -g $RESOURCE_GROUP --query `"properties.configuration.ingress.fqdn`""
Write-Host ""
Write-Host "To verify runtime image:"
Write-Host "  az containerapp show -n $APP_NAME -g $RESOURCE_GROUP --query `"properties.template.containers[].image`""
Write-Host ""
