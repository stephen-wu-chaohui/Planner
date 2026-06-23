# Planner New Subscription Deployment

This runbook rebuilds the dev deployment in a new Azure subscription.

## Prerequisites

- Azure CLI logged in to the new subscription.
- Permissions to create resource groups, role assignments, app registrations, and Key Vault secrets.
- `gh` authenticated to `stephen-wu-chaohui/Planner`.
- Access to the existing `plannerdemo.com` DNS provider.
- Fresh Google/Firebase/Gemini/Maps credentials from the existing Google/Firebase project.

## 1. Deploy Azure Infrastructure

Preview:

```powershell
./scripts/azure/deploy-infra.ps1 -WhatIf
```

Deploy:

```powershell
./scripts/azure/deploy-infra.ps1
```

This creates `rg-planner-dev-aue`, ACR, Key Vault, Log Analytics/App Insights, Container Apps, RabbitMQ with Azure Files storage, Azure SQL, App Service, and DbMigrator jobs.

## 2. Bootstrap Entra ID

```powershell
./scripts/azure/bootstrap-entra.ps1 -DemoPassword "<temporary-demo-user-password>"
```

This creates or updates:

- `planner-dev-api` with `API.Access`.
- `planner-dev-blazor` with `https://planner.plannerdemo.com/signin-oidc`.
- `planner-dev-github-deploy` with GitHub OIDC subject `repo:stephen-wu-chaohui/Planner:environment:dev`.
- Demo admin users for the seeded tenants.

It stores app ids, the Blazor client secret, and the API scope in Key Vault.

## 3. Store Rotated Runtime Secrets

```powershell
./scripts/azure/set-runtime-secrets.ps1 `
  -FirestoreProjectId "<project-id>" `
  -FirebaseConfigJsonPath "<service-account.json>" `
  -GoogleApiKey "<google-api-key>" `
  -GoogleMapsApiKey "<maps-key>" `
  -GoogleMapsMapId "<map-id>" `
  -GeminiApiKey "<gemini-key>"
```

## 4. Configure GitHub

```powershell
./scripts/azure/configure-github.ps1
```

GitHub environment `dev` receives only the Azure OIDC secrets. Resource names are stored as environment variables.

## 5. Configure DNS And Certificates

Print required DNS records:

```powershell
./scripts/azure/configure-custom-domains.ps1
```

Create these records with the current DNS provider:

- `planner` CNAME to the App Service default hostname.
- `asuid.planner` TXT to the App Service verification id.
- `api` CNAME to the Container Apps hostname.
- `asuid.api` TXT to the Container Apps verification id.

After DNS propagates:

```powershell
./scripts/azure/configure-custom-domains.ps1 -Bind
```

## 6. Deploy Images And Database

Run the GitHub workflows in this order:

1. `Deploy Planner.API to Azure Container Apps (dev)`
2. `Deploy planner.optimization.worker to Azure Container Apps`
3. `Deploy Planner.AI to Azure Container Apps`
4. `Deploy Planner.BlazorApp (dev)`
5. `Database Migrate & Seed (dev)` with `migrate-and-seed`

## 7. Smoke Test

- `https://api.plannerdemo.com/health` returns healthy.
- `https://api.plannerdemo.com/graphql` loads.
- `https://planner.plannerdemo.com` redirects through Entra login.
- Sign in as `auckland.admin@plannerdemo.com`.
- Submit an optimization request and verify API -> RabbitMQ -> worker -> Firestore -> AI insight -> Blazor display.
