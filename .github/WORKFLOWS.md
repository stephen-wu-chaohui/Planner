# CI/CD Overview

This repository uses separate workflows for database and application deployment.
The Azure dev deployment is bootstrapped by Bicep and scripts under `infra/` and `scripts/azure/`.

## Dev environment

- `db-migrator-dev.yml`
  - Builds/pushes the `Planner.Tools.DbMigrator` container image to ACR
  - Runs the Azure Container Apps migration job
  - Runs the Azure Container Apps seed job when requested
  - Mutates Azure SQL (dev) only

- `deploy-planner-api-aca.yml`
  - Builds and pushes the `Planner.API` container image to ACR
  - Deploys/updates Planner.API to Azure Container Apps (dev)
  - Does not mutate the database

- `deploy-planner-optimization-worker-aca.yml`
  - Builds and pushes the `Planner.Optimization.Worker` container image to ACR
  - Deploys/updates the worker to Azure Container Apps (dev)

- `deploy-planner-ai-worker.yml`
  - Builds and pushes the `Planner.AI` container image to ACR
  - Deploys/updates the AI worker to Azure Container Apps (dev)

- `main_planner-blazor-dev.yml`
  - Builds/publishes `Planner.BlazorApp`
  - Generates the Blazor WebAssembly `wwwroot/appsettings.json` from GitHub environment variables before publishing
  - Deploys the published static `wwwroot` assets to Azure App Service (dev), served by the Linux Node/PM2 static host

The API never performs schema migration or data seeding at runtime.
All database changes are explicit and executed via DbMigrator.

GitHub Actions uses OIDC against the `dev` environment. Runtime settings and secrets live in Azure Key Vault;
GitHub stores only `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and `AZURE_SUBSCRIPTION_ID` as environment secrets.
The WebAssembly app is static, so browser-visible frontend settings are stored as GitHub environment variables:

- `BLAZOR_API_BASE_URL`
- `BLAZOR_API_SCOPE`
- `BLAZOR_AZURE_AD_AUTHORITY`
- `BLAZOR_AZURE_AD_CLIENT_ID`
- `BLAZOR_GOOGLE_MAPS_API_KEY`
- `BLAZOR_GOOGLE_MAPS_MAP_ID` (optional)

These values are written into the published `Planner.BlazorApp` config. Do not put server secrets or client secrets in those variables.
