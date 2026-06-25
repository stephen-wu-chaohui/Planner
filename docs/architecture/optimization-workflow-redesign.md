We are refactoring Planner from the current RabbitMQ request/result flow into an Azure-native asynchronous optimization architecture.

Current high-level behavior:
- API currently sends optimization requests to RabbitMQ.
- Worker consumes the request, solves the route optimization, and sends the result back through RabbitMQ.
- API persists the result for BlazorApp / AI Worker consumption.
- SQL Server + EF Core already store Tenants, Vehicles, Depots, Jobs, and Routes.

Target architecture:
- Keep SQL Server + EF Core as the source of truth for long-lived master data: Tenants, Vehicles, Depots, Jobs, accepted/published Routes.
- Add Cosmos DB as the source of truth for each OptimizationRun lifecycle: request snapshot, status, solver result, AI insight, timeline, execution attempts.
- Add Azure Service Bus as the command queue. The API sends only { tenantId, optimizationRunId }.
- Add a Container Apps Job-style worker project that consumes one Service Bus message, loads the OptimizationRun from Cosmos DB, calls the existing VehicleRoutingProblem.Optimize(request), saves the result/status back to Cosmos DB, and completes/abandons/dead-letters the message appropriately.
- Add a Planner.Reactor Functions App that listens to Cosmos DB Change Feed and publishes lightweight SignalR notifications.
- BlazorApp receives SignalR notifications, updates local summary state, and calls Planner.Api for full result only when needed.
- API remains the user-facing command/query control plane.
- Planner.Reactor is not a user-facing query API.

Please inspect the repository and produce an implementation plan only. Do not modify files yet.

The plan must include:
1. Existing projects/files that should be changed.
2. New projects that should be added.
3. Proposed data contracts:
   - OptimizationRunDocument
   - OptimizationRunStatus
   - OptimizationJobMessage
   - SignalR event DTOs
4. Proposed service interfaces:
   - IOptimizationRunStore
   - IOptimizationJobQueue
   - IOptimizationRunSnapshotBuilder
5. Proposed local development setup.
6. A safe migration strategy that keeps the existing RabbitMQ path working until the new path is verified.
7. Test plan:
   - unit tests
   - integration tests
   - local manual test steps
8. Risks and open questions.

Important constraints:
- Do not remove the existing RabbitMQ implementation in the first PR.
- Do not move Tenant/Vehicles/Depots/Jobs master data out of SQL Server.
- Do not make Functions App a middleman between API and Worker.
- Worker must not call API to get request data or return result.
- Worker reads/writes Cosmos DB directly.
- SignalR messages must be lightweight; BlazorApp should call API for full result.
