# Planner Agent Instructions

## Optimization Worker Architecture

`Planner.Optimization.Worker` is a long-running background worker, not a one-shot job worker.

It should support both messaging channels:

```text
Primary Azure path:
Planner API -> Azure Service Bus -> Planner.Optimization.Worker

Playground / legacy path:
Planner API -> RabbitMQ -> Planner.Optimization.Worker
```

So far, let's keep Playground / legacy path until changes made in this file.

### Design principle

The worker should keep the optimization processing pipeline channel-agnostic.

Service Bus and RabbitMQ should only differ in message transport/adapters. The core optimization flow should remain shared:

```text
Receive optimization job message
Load optimization run/input
Run solver
Upload result to Planner API
API persists result
API sends SignalR notification
Blazor fetches latest result from API
```

### Responsibilities

#### Planner API

The API owns optimization run state and client notifications.

It should:

* Create optimization runs.
* Save run/input data.
* Publish lightweight optimization job messages to the configured transport.
* Receive completed solver results from `Planner.Optimization.Worker`.
* Persist solver results.
* Update run status.
* Notify Blazor clients through SignalR.
* Expose GET endpoints for Blazor to fetch the latest run/result data.

#### Planner.Optimization.Worker

The worker owns computation only.

It should:

* Run as a long-lived `BackgroundService`.
* Listen continuously to the configured message transport.
* Support Azure Service Bus as the primary Azure-native transport.
* Preserve RabbitMQ support as a playground / legacy transport.
* Receive optimization job messages.
* Load the required optimization run/input data.
* Execute the solver.
* Upload the solver result back to Planner API.
* Acknowledge/complete the message only after the API confirms the result was saved successfully.
* Reject, abandon, requeue, or dead-letter messages according to the transport and failure type.

The worker should not directly notify Blazor and should not directly send SignalR messages.

### Important rule

Do not acknowledge or complete a message before the solver result has been successfully uploaded to the API and persisted by the API.

Correct order:

```text
Receive message
Run solver
Upload result to API
API saves result
API returns success
Acknowledge / complete message
```

### Local development

In local development, message brokers do not start the worker process.

Run both services manually:

```bash
dotnet run --project Planner.Api
dotnet run --project Planner.Optimization.Worker
```

The worker should remain running and continue listening for messages.

### Transport configuration

Use configuration to select the active optimization messaging transport.

Example:

```json
{
  "OptimizationMessaging": {
    "Transport": "ServiceBus"
  }
}
```

Supported values:

```text
ServiceBus
RabbitMQ
```

The transport-specific code should be isolated behind interfaces/adapters. The solver and result-upload workflow should not depend directly on Service Bus or RabbitMQ SDK types.

### Azure deployment direction

In Azure, prefer `ServiceBus` as the primary transport.

Deploy `Planner.Optimization.Worker` as a long-running Container App worker.

Use Service Bus queue depth and Container Apps/KEDA scaling rules to scale worker replicas when needed.

Keep `RabbitMQ` available for local experimentation, architecture comparison, and playground scenarios.


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
