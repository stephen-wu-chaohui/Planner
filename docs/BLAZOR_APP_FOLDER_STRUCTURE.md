# Planner.BlazorApp Folder Structure

This document describes the current folder structure of Planner.BlazorApp, a **Blazor WebAssembly** frontend application.

## Architecture Overview

Planner.BlazorApp runs as a **standalone Blazor WebAssembly** application. All application code is compiled to WebAssembly and executed in the browser. There is no server-side Blazor rendering.

- **Authentication**: Azure AD / Entra ID via MSAL (Microsoft Authentication Library for JavaScript), configured through `Microsoft.Authentication.WebAssembly.Msal`.
- **API calls**: All backend communication is via HTTP REST calls to `Planner.API`, with Bearer tokens obtained from MSAL.
- **Real-time updates**: Optimization results are received via HTTP polling against `GET /api/vrp/results/{runId}` (replacing the previous Firestore listener).
- **Configuration**: Application settings are loaded from `wwwroot/appsettings.json` at runtime.

## Directory Structure

```
Planner.BlazorApp/
├── Auth/                           # Authentication services
│   ├── AuthorizationMessageHandler.cs  # MSAL-based bearer token handler for HTTP calls
│   └── RedirectToLogin.razor           # Redirects unauthenticated users to MSAL login
│
├── Components/                     # Blazor components
│   ├── Auth/                       # Authentication-related UI components
│   │   ├── DemoLoginModal.razor    # Demo login modal with city account selector
│   │   └── RedirectToLogin.razor   # Auto-redirects to MSAL login
│   │
│   ├── DispatchCenter/            # Feature: Dispatch Center components
│   │   ├── CustomersTab.razor
│   │   ├── DispatchCenter.razor
│   │   ├── JobsTab.razor
│   │   ├── NewCustomerModal.razor
│   │   ├── PlannerEntitiesModal.razor
│   │   ├── PlannerMap.razor
│   │   ├── RoutesBuildPanel.razor
│   │   ├── RoutesTab.razor
│   │   └── VehiclesTab.razor
│   │
│   ├── Pages/                     # Page components
│   │   ├── Authentication.razor   # MSAL authentication callback handler
│   │   └── Error.razor
│   │
│   ├── Shared/                    # Shared, reusable components
│   │   └── Layout/               # Layout components
│   │       ├── MainLayout.razor
│   │       └── NavMenu.razor
│   │
│   ├── WelcomeWizard/            # Feature: Welcome Wizard
│   │   ├── WelcomeWizardModal.razor
│   │   └── Wizard/
│   │       ├── WelcomeWizardDefinition.cs
│   │       ├── WizardService.cs
│   │       └── WizardStep.cs
│   │
│   ├── App.razor                  # Root app component (router + auth state)
│   ├── Routes.razor               # Legacy routing component (kept for reference)
│   └── _Imports.razor             # Global component imports
│
├── FormModels/                    # Models and DTOs for forms
│   ├── CustomerFormModel.cs
│   ├── CustomerMarker.cs
│   ├── EditableFlags.cs
│   ├── JobFormModel.cs
│   ├── LoginFormModel.cs
│   ├── MapRoute.cs
│   └── VehicleFormModel.cs
│
├── Services/                      # Application services
│   ├── ColourHelper.cs
│   ├── EditStyles.cs
│   ├── OptimizationResultsListenerService.cs    # Firestore-based (used by server-side if needed)
│   ├── PollingOptimizationResultsListenerService.cs  # WASM-compatible HTTP polling
│   ├── RouteInsightsListenerService.cs          # Firestore-based + NoOp WASM implementation
│   └── PlannerApiClient.cs
│
├── State/                         # State management with partial classes
│   ├── DispatchCenterState.cs
│   ├── DispatchCenterState.Customer.cs   # Partial class
│   ├── DispatchCenterState.Insights.cs   # Partial class
│   ├── DispatchCenterState.Job.cs        # Partial class
│   ├── DispatchCenterState.Processing.cs # Partial class
│   ├── DispatchCenterState.Routes.cs     # Partial class
│   ├── DispatchCenterState.Tenant.cs     # Partial class
│   ├── DispatchCenterState.Vehicle.cs    # Partial class
│   │
│   └── Interfaces/                # State interfaces
│       ├── ICustomerState.cs
│       ├── IDispatchStateProcessing.cs
│       ├── IInsightState.cs
│       ├── IJobState.cs
│       ├── IRouteState.cs
│       ├── ITenantState.cs
│       └── IVehicleState.cs
│
├── wwwroot/                       # Static web resources
│   ├── bootstrap/                # CSS frameworks
│   ├── css/                      # Custom CSS files
│   ├── data/                     # Static data files
│   ├── icons/                    # Icon assets
│   ├── images/                   # Image assets
│   ├── js/                       # JavaScript files
│   │   ├── plannerMap.js         # Google Maps integration (dynamically loads Maps API)
│   │   └── wizardStorage.js      # Wizard local storage helper
│   ├── app.css                   # Main application CSS
│   ├── appsettings.json          # WASM runtime configuration (AzureAd, Api, GoogleMaps)
│   ├── favicon.png               # Favicon
│   └── index.html                # WASM entry point (HTML shell)
│
├── Program.cs                     # Application entry point (WebAssemblyHostBuilder)
├── Planner.BlazorApp.csproj      # Project file (Microsoft.NET.Sdk.BlazorWebAssembly)
├── appsettings.json              # Legacy server config (not used by WASM)
└── appsettings.Development.json  # Legacy server config (not used by WASM)
```

## Best Practices Implemented

### 1. Component Organization
- **Feature-based folders**: Components are grouped by feature (DispatchCenter/, WelcomeWizard/)
- **Shared components**: Reusable components are in Components/Shared/
- **Layout separation**: Layout components are in Components/Shared/Layout/
- **Auth components**: Authentication UI is in Components/Auth/

### 2. State Management
- **Partial classes**: DispatchCenterState is split into logical partial classes (Customer, Job, Routes, Vehicle, etc.)
- **Interface separation**: All state interfaces are in State/Interfaces/
- **Clear responsibilities**: Each partial class handles a specific domain concern

### 3. Models and DTOs
- **Centralized location**: All form models and DTOs are in FormModels/
- **Consistent naming**: All models follow PascalCase convention
- **Clear purpose**: Models are specifically for form data binding

### 4. Services
- **Single location**: All services are in Services/
- **Interface pattern**: Services follow interface/implementation pattern
- **WASM-compatible implementations**: Polling-based services replace Firestore listeners for browser compatibility

### 5. Authentication
- **MSAL-based**: Authentication uses `Microsoft.Authentication.WebAssembly.Msal` for browser-side Azure AD auth
- **Token handler**: `AuthorizationMessageHandler` uses `IAccessTokenProvider` to attach Bearer tokens to API calls
- **Auth pages**: `Components/Pages/Authentication.razor` handles MSAL redirect callbacks

### 6. Static Resources
- **Organized by type**: wwwroot/ contains only static files organized by type (css/, js/, images/, icons/)
- **WASM entry point**: `wwwroot/index.html` is the HTML shell for the WASM app
- **Runtime config**: `wwwroot/appsettings.json` provides configuration loaded at runtime by the browser

### 7. Naming Conventions
- **PascalCase**: All components, services, and interfaces use PascalCase
- **Descriptive names**: Names clearly indicate purpose and responsibility
- **Consistent suffixes**: Components end in component type (e.g., Modal, Tab, Panel)

## Migration Notes

### Blazor Server → Blazor WebAssembly Migration

The following changes were made to migrate from Blazor Server to standalone Blazor WebAssembly:

**Project Configuration:**
- SDK changed from `Microsoft.NET.Sdk.Web` to `Microsoft.NET.Sdk.BlazorWebAssembly`
- Removed server-side packages: `Microsoft.AspNetCore.Authentication.OpenIdConnect`, `Microsoft.Identity.Web`, `Microsoft.Identity.Web.UI`, `Google.Cloud.Firestore`
- Added WASM packages: `Microsoft.AspNetCore.Components.WebAssembly`, `Microsoft.Authentication.WebAssembly.Msal`, `Microsoft.Extensions.Http`
- Removed project references to `Planner.Application` and `Planner.Messaging` (server-side infrastructure)

**Authentication:**
- Replaced server-side OpenIdConnect + OIDC cookie auth with client-side MSAL
- `AuthorizationMessageHandler` now uses `IAccessTokenProvider` instead of `ITokenAcquisition`
- Sign-in/sign-out now uses MSAL routes (`authentication/login`, `authentication/logout`)
- `DemoLoginModal` passes `login_hint` via `InteractiveRequestOptions.TryAddAdditionalParameter`

**Entry Point:**
- Created `wwwroot/index.html` as the WASM HTML shell
- `Program.cs` rewritten to use `WebAssemblyHostBuilder` instead of `WebApplication`
- `App.razor` changed from an HTML shell to a Blazor router component

**Real-time Updates:**
- Firestore listeners replaced with `PollingOptimizationResultsListenerService` (HTTP polling via `GET /api/vrp/results/{runId}`)
- `NoOpRouteInsightsListenerService` replaces Firestore listener for AI route insights (pending SignalR upgrade)
- New `GET /api/vrp/results/{runId}` endpoint added to `Planner.API`

**Components:**
- Removed `@rendermode InteractiveServer` from all components (not needed in WASM)
- Removed `@using static Microsoft.AspNetCore.Components.Web.RenderMode` imports
- `Error.razor` updated to remove server-side `HttpContext` dependency
- `ColourHelper.cs` updated to use browser-compatible djb2 hash instead of `MD5`

## Configuration

The WASM app reads configuration from `wwwroot/appsettings.json`. Key settings:

```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/{TenantId}",
    "ClientId": "{your-client-id}",
    "ValidateAuthority": true
  },
  "Api": {
    "BaseUrl": "https://your-api-url",
    "Scope": "api://{client-id}/access_as_user"
  },
  "GoogleMaps": {
    "ApiKey": "{your-google-maps-api-key}",
    "MapId": "{your-map-id}"
  }
}
```

## References

- [Blazor WebAssembly standalone apps](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models#blazor-webassembly)
- [Secure ASP.NET Core Blazor WebAssembly with Azure Active Directory](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/standalone-with-azure-active-directory)
- [Blazor Project Structure Best Practices](https://learn.microsoft.com/en-us/aspnet/core/blazor/project-structure)
- [ASP.NET Core Blazor layouts](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/layouts)

