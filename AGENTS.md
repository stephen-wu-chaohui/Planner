# Planner Agent Instructions

## Architecture direction

Planner is a multi-tenant route optimization platform.

The target architecture is:

- SQL Server + EF Core remain the source of truth for long-lived master data:
  - Tenants
  - Vehicles
  - Depots
  - Jobs
  - accepted/published Routes

- Cosmos DB stores optimization run lifecycle documents:
  - request snapshot
  - status
  - solver result
  - AI insight
  - timeline
  - execution attempts

- Azure Service Bus is the command queue.
  - Messages should be lightweight.
  - Optimization job messages must contain tenantId and optimizationRunId only.
  - Do not put full OptimizeRouteRequest payloads into Service Bus messages.

- Optimization execution is handled by a Container Apps Job-style worker.
  - The worker consumes Service Bus messages.
  - The worker reads OptimizationRun documents from Cosmos DB.
  - The worker calls VehicleRoutingProblem.Optimize(...)
  - The worker writes status/result back to Cosmos DB.
  - The worker must not call Planner.Api to fetch requests or return results.

- Planner.Reactor / Functions App is a reaction layer around Cosmos DB.
  - It listens to Cosmos DB Change Feed.
  - It publishes lightweight SignalR notifications.
  - It may handle AI insight triggers, DLQ processing, cleanup, and stuck-run recovery.
  - It must not become a user-facing query API.

- Planner.Api remains the user-facing command/query control plane.
  - It handles authentication, tenant authorization, quota checks, run creation, status queries, result queries, and route acceptance.
  - BlazorApp must call Planner.Api for full result data.
  - SignalR messages should notify that something changed; they should not be treated as the authoritative data source.

## Multi-tenancy rules

- Tenant isolation is mandatory.
- All SQL queries must be tenant-scoped.
- Cosmos DB OptimizationRuns must use tenantId as the partition key.
- Service Bus messages must include tenantId.
- SignalR notifications must be tenant-scoped.
- Never implement a method that retrieves tenant data by runId alone without tenantId.

## Migration rules

- Do not remove the existing RabbitMQ workflow until the new Service Bus + Cosmos workflow is verified.
- Prefer feature flags for switching between old and new workflow paths.
- Keep changes incremental and testable.

## Blazor rules

- SignalR messages should update local summary state.
- Blazor should call Planner.Api for full result/AI insight data only when needed.
- Use @key for rendered run lists, using OptimizationRunId.
- Avoid automatically pulling full result data for every SignalR notification.

## Testing expectations

Before completing a task, run:

```bash
dotnet build
dotnet test

```

## Shipit
Read and work as defined in docs\codex-workflows.md
