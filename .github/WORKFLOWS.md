# CI/CD Overview

This repository uses separate workflows for database and application deployment.

## Dev environment

- `db-migrator-dev.yml`
  - Applies EF Core migrations
  - Executes SQL seed scripts
  - Mutates Azure SQL (dev) only

- `deploy-planner-api-aca.yml`
  - Builds and pushes the `Planner.API` container image to ACR
  - Deploys/updates Planner.API to Azure Container Apps (dev)
  - Does not mutate the database

- `deploy-planner-optimization-worker-aca.yml`
  - Builds and pushes the `Planner.Optimization.Worker` container image to ACR
  - Deploys/updates the worker to Azure Container Apps (dev)

- `main_planner-blazor-dev.yml`
  - Builds/publishes `Planner.BlazorApp`
  - Deploys to Azure App Service (dev)

The API never performs schema migration or data seeding at runtime.
All database changes are explicit and executed via DbMigrator.
