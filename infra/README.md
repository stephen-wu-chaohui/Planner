# Planner Azure Dev Infrastructure

This folder contains the Bicep entrypoint for rebuilding Planner in a new Azure subscription.

## Deploy

```powershell
./scripts/azure/deploy-infra.ps1
```

Use `-WhatIf` first when checking a subscription:

```powershell
./scripts/azure/deploy-infra.ps1 -WhatIf
```

The script creates or updates `rg-planner-dev-aue` in `australiaeast`, generates SQL/RabbitMQ passwords for the first deployment, and reuses those values from Key Vault on later runs.

## Outputs

After a successful deployment, the script writes `infra/.last-deployment-outputs.json` locally. That file is intentionally ignored because it contains environment-specific resource names.

## Follow-Up Scripts

Run these after infrastructure deployment:

```powershell
./scripts/azure/bootstrap-entra.ps1
./scripts/azure/set-runtime-secrets.ps1 -FirestoreProjectId "<project-id>" -FirebaseConfigJsonPath "<service-account.json>" -GoogleApiKey "<google-api-key>" -GoogleMapsApiKey "<maps-key>" -GoogleMapsMapId "<map-id>" -GeminiApiKey "<gemini-key>"
./scripts/azure/configure-github.ps1
./scripts/azure/configure-custom-domains.ps1
```

After DNS records are visible, bind the custom domains:

```powershell
./scripts/azure/configure-custom-domains.ps1 -Bind
```
