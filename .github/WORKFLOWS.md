# CI/CD Overview

This repository uses separate workflows for database and application deployment.

## Dev environment

- `db-migrator-dev.yml`
  - Applies EF Core migrations
  - Executes SQL seed scripts
  - Mutates Azure SQL (dev) only

- `main_planner-api-dev.yml`
  - Builds and deploys Planner.API
  - Does not mutate the database

The API never performs schema migration or data seeding at runtime.
All database changes are explicit and executed via DbMigrator.
