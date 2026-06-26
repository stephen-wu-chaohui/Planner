# Planner.BlazorApp Folder Structure

`Planner.BlazorApp` is a Blazor WebAssembly frontend. It runs in the browser, authenticates with MSAL, calls `Planner.API` through `PlannerApiClient`, and listens for lightweight optimization change notifications over SignalR.

## Directory Structure

```text
Planner.BlazorApp/
|-- Auth/
|   `-- AuthorizationMessageHandler.cs
|-- Components/
|   |-- App.razor
|   |-- Routes.razor
|   |-- _Imports.razor
|   |-- Auth/
|   |   `-- DemoLoginModal.razor
|   |-- DispatchCenter/
|   |   |-- DispatchCenter.razor
|   |   |-- PlannerMap.razor
|   |   `-- Models/
|   |       |-- CustomersTab.razor
|   |       |-- InsightsTab.razor
|   |       |-- JobsTab.razor
|   |       |-- PlannerEntitiesModal.razor
|   |       |-- RoutesBuildPanel.razor
|   |       |-- RoutesTab.razor
|   |       `-- VehiclesTab.razor
|   |-- Pages/
|   |   |-- Authentication.razor
|   |   `-- Error.razor
|   |-- Shared/Layout/
|   |   |-- MainLayout.razor
|   |   `-- NavMenu.razor
|   `-- WelcomeWizard/
|-- FormModels/
|-- Services/
|   |-- PlannerApiClient.cs
|   |-- OptimizationResultsListenerService.cs
|   |-- SignalROptimizationResultsListenerService.cs
|   `-- RouteInsightsListenerService.cs
|-- State/
|   |-- DispatchCenterState*.cs
|   `-- Interfaces/
|-- wwwroot/
|   |-- index.html
|   |-- appsettings.json
|   |-- appsettings.Development.json
|   |-- app.css
|   |-- bootstrap/
|   |-- css/
|   |-- icons/
|   |-- images/
|   `-- js/
|-- Program.cs
|-- Planner.BlazorApp.csproj
`-- web.config
```

## Key Conventions

- Browser-visible runtime config lives under `wwwroot/appsettings*.json`.
- Server secrets and client secrets must not be added to WebAssembly config files.
- `Authentication.razor` hosts `RemoteAuthenticatorView` for MSAL login/logout callbacks.
- `PlannerApiClient` is the only general HTTP client wrapper for API calls.
- SignalR messages notify that an optimization run changed; full result and AI insight data is fetched from `Planner.API`.
- State remains split across `DispatchCenterState` partial classes by domain area.

## Deployment Notes

The Azure Blazor workflow generates the published `wwwroot/appsettings.json` from GitHub environment variables before `dotnet publish`. Local development uses `wwwroot/appsettings.Development.json`.
