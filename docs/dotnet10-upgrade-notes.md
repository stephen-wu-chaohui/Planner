# Planner .NET 10 Upgrade Notes

## Scope

This phase upgrades Planner from .NET 8 to .NET 10 as a framework uplift only.

- Upgraded all `src/*`, `tools/*`, and `test/*` .NET projects from `net8.0` to `net10.0`.
- Updated framework-related NuGet dependencies for .NET 10 compatibility.
- Updated Dockerfiles and CI workflows that assumed .NET 8.
- Kept SQL Server provider and Redis cache architecture unchanged.
- Deferred PostgreSQL migration and HybridCache migration by design.

## Retargeted Projects

All 22 project files under `src`, `tools`, and `test` now target `net10.0`:

- `src/Planner.API/Planner.API.csproj`
- `src/Planner.Application/Planner.Application.csproj`
- `src/Planner.BlazorApp/Planner.BlazorApp.csproj`
- `src/Planner.Contracts/Planner.Contracts.csproj`
- `src/Planner.Domain/Planner.Domain.csproj`
- `src/Planner.Infrastructure/Planner.Infrastructure.csproj`
- `src/Planner.Messaging/Planner.Messaging.csproj`
- `src/Planner.Optimization.Contracts/Planner.Optimization.Contracts.csproj`
- `src/Planner.Optimization.Worker/Planner.Optimization.Worker.csproj`
- `src/Planner.Optimization/Planner.Optimization.csproj`
- `tools/Planner.Tools.DbMigrator/Planner.Tools.DbMigrator.csproj`
- `test/Planner.API.EndToEndTests/Planner.API.EndToEndTests.csproj`
- `test/Planner.API.Tests/Planner.API.Tests.csproj`
- `test/Planner.BlazorApp.Tests/Planner.BlazorApp.Tests.csproj`
- `test/Planner.Infrastructure.Tests/Planner.Infrastructure.Tests.csproj`
- `test/Planner.Messaging.ContractSnapshots/Planner.Messaging.ContractSnapshots.csproj`
- `test/Planner.Optimization.IntegrationTests/Planner.Optimization.IntegrationTests.csproj`
- `test/Planner.Optimization.PropertyTests/Planner.Optimization.PropertyTests.csproj`
- `test/Planner.Optimization.SnapshotTests/Planner.Optimization.SnapshotTests.csproj`
- `test/Planner.Optimization.Tests/Planner.Optimization.Tests.csproj`
- `test/Planner.Optimization.Worker.Tests/Planner.Optimization.Worker.Tests.csproj`
- `test/Planner.Testing/Planner.Testing.csproj`

## SDK / Runtime Baseline

- Added `global.json` with SDK pin:
  - `10.0.201`

## Package Upgrades (Key Changes)

### EF Core / Data Access

- `Microsoft.EntityFrameworkCore` `8.0.12` -> `10.0.5`
- `Microsoft.EntityFrameworkCore.SqlServer` `8.0.8` -> `10.0.5`
- `Microsoft.EntityFrameworkCore.Design` `8.0.12` -> `10.0.5`
- `Microsoft.EntityFrameworkCore.InMemory` `8.x` -> `10.0.5` (test projects)
- `Microsoft.Data.SqlClient` `5.2.2` -> `7.0.0`

### ASP.NET Core / Auth / SignalR / Hosting

- `Microsoft.AspNetCore.Authentication.JwtBearer` `8.0.8` -> `10.0.5`
- `Microsoft.AspNetCore.Authentication.OpenIdConnect` `8.0.0` -> `10.0.5`
- `Microsoft.AspNetCore.SignalR.Client` `8.0.1` -> `10.0.5`
- `Microsoft.AspNetCore.Mvc.Testing` `8.0.11` -> `10.0.5`
- `Microsoft.Extensions.*` packages used by Messaging/Optimization/Worker -> `10.0.5`
- `Microsoft.Identity.Web` / `Microsoft.Identity.Web.UI` `4.3.0` -> `4.6.0`

### Messaging / Misc

- `Google.Cloud.Firestore` `3.7.0/4.1.0` -> `4.2.0`
- `Newtonsoft.Json` `13.0.3` -> `13.0.4`
- `System.IdentityModel.Tokens.Jwt` `8.15.0` -> `8.16.0`
- `RabbitMQ.Client` left at `6.8.1` in this phase (no architecture/API migration)

### Test Stack

- `coverlet.collector` `6.0.2` -> `8.0.1`
- `FluentAssertions` `8.8.0` -> `8.9.0`
- `Microsoft.NET.Test.Sdk` normalized to `17.12.0` (stable with current xUnit v2 setup)
- `Verify.Xunit` `31.9.3` -> `31.12.5`
- `xunit` `2.9.2/2.9.3` -> `2.9.3`
- `xunit.runner.visualstudio` `2.5.8/2.8.2` -> `2.8.2`

### Removed Redundant Package References (NU1510 cleanup)

- Removed explicit `System.Text.Json` package references from:
  - `src/Planner.Infrastructure/Planner.Infrastructure.csproj`
  - `src/Planner.BlazorApp/Planner.BlazorApp.csproj`
- Removed explicit `Microsoft.Extensions.Configuration` package reference from:
  - `src/Planner.BlazorApp/Planner.BlazorApp.csproj`

## Docker / CI Updates

### Dockerfiles

- `src/Planner.API/Dockerfile`
  - `mcr.microsoft.com/dotnet/sdk:8.0` -> `:10.0`
  - `mcr.microsoft.com/dotnet/aspnet:8.0` -> `:10.0`
- `src/Planner.Optimization.Worker/Dockerfile`
  - `mcr.microsoft.com/dotnet/sdk:8.0` -> `:10.0`
  - `mcr.microsoft.com/dotnet/runtime:8.0` -> `:10.0`

### CI Workflows

- `.github/workflows/main_planner-blazor-dev.yml`
  - `actions/setup-dotnet` `8.0.x` -> `10.0.x`
- `.github/workflows/db-migrator-dev.yml`
  - `actions/setup-dotnet` `8.0.x` -> `10.0.x`

### Docs / Prerequisites

- Updated `.NET 8` -> `.NET 10` references in:
  - `README.md`
  - `docs/README.md`
  - `assets/banner-dark.svg`
  - `assets/banner-light.svg`

## Code Changes for .NET 10 Compatibility

### ASP.NET Core deprecation fix

- `src/Planner.API/Program.cs`
  - Replaced `ForwardedHeadersOptions.KnownNetworks` usage with `KnownIPNetworks` to address ASP.NET Core 10 obsoletion (`ASPDEPR005`).

### Optimization runtime safety fix

- `src/Planner.Optimization/VehicleRoutingProblem.cs`
  - Hardened `Time` dimension window assignment:
    - skip invalid/unassigned routing indices
    - clamp time-window upper bound to horizon
    - clamp lower bound to upper bound
  - This prevents native OR-Tools crashes during test execution on .NET 10.

## Build / Test Status

### Build

- `dotnet restore Planner.sln` ✅
- `dotnet build Planner.sln -c Release -t:Rebuild --no-restore` ✅
- No .NET 10 framework/deprecation/pruning warnings remain (NU1510/ASPDEPR005 addressed).

### Test

- Passing:
  - `Planner.API.EndToEndTests` (7 passed)
  - `Planner.Optimization.IntegrationTests` (5 passed)
  - `Planner.Optimization.PropertyTests` (4 passed)
  - `Planner.Infrastructure.Tests` (6 passed)
  - `Planner.BlazorApp.Tests` (1 passed)
- Not containing tests (informational):
  - `Planner.API.Tests`
  - `Planner.Testing`
  - `Planner.Optimization.Tests`
  - `Planner.Optimization.Worker.Tests`
- Failing (snapshot deltas):
  - `Planner.Optimization.SnapshotTests`
  - `Planner.Messaging.ContractSnapshots`

Snapshot failures are currently due to verified expectations asserting a prior `"Matrix dimension mismatch"` payload while current output is an empty-route response payload.

## Breaking Changes Review (Relevant to Planner)

Reviewed and applied/assessed:

- .NET 10 compatibility overview  
  https://learn.microsoft.com/en-us/dotnet/core/compatibility/10.0
- EF Core 10 breaking changes (EF10 requires .NET 10 SDK/runtime)  
  https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/breaking-changes  
  https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew
- ASP.NET Core 10 breaking changes overview  
  https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/10/overview?view=aspnetcore-10.0
- Forwarded headers obsoletion (`KnownNetworks` -> `KnownIPNetworks`)  
  https://aka.ms/aspnet/deprecate/005
- .NET 10 SDK pruning warning change (NU1510)  
  https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/10.0/nu1510-pruned-references
- Hosted service behavior change (`BackgroundService.ExecuteAsync` fully background-threaded in .NET 10)  
  https://learn.microsoft.com/en-us/dotnet/core/compatibility/extensions/10.0/backgroundservice-executeasync-task

## Manual Follow-Ups

1. Decide on snapshot baseline direction:
   - update `*.verified.*` files to current behavior, or
   - adjust optimization input/build logic to preserve previous expected payloads.
2. Review `BackgroundService` startup ordering expectations:
   - `src/Planner.API/BackgroundServices/OptimizeRouteResultConsumer.cs`
   - `src/Planner.Optimization.Worker/BackgroundServices/OptimizationWorker.cs`
3. Optional hardening cleanup (non-blocking for framework uplift):
   - Existing nullable warnings in API/Blazor/Infrastructure/Messaging.
   - Firestore credential obsolescence warning (`JsonCredentials`).

## Intentionally Deferred

- SQL Server -> PostgreSQL migration.
- Redis -> HybridCache migration.
- Any architecture redesign or large behavior changes beyond framework uplift.

