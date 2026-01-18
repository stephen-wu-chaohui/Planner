# Planner.BlazorApp Folder Structure

This document describes the current folder structure of Planner.BlazorApp, which follows Blazor best practices.

## Directory Structure

```
Planner.BlazorApp/
├── Auth/                           # Authentication services
│   ├── IJwtTokenStore.cs          # JWT token store interface
│   └── JwtTokenStore.cs           # JWT token store implementation
│
├── Components/                     # Blazor components
│   ├── DispatchCenter/            # Feature: Dispatch Center components
│   │   ├── CustomersTab.razor
│   │   ├── DispatchCenter.razor
│   │   ├── JobsTab.razor
│   │   ├── Login.razor
│   │   ├── NewCustomerModal.razor
│   │   ├── PlannerEntitiesModal.razor
│   │   ├── PlannerMap.razor
│   │   ├── RoutesBuildPanel.razor
│   │   ├── RoutesTab.razor
│   │   └── VehiclesTab.razor
│   │
│   ├── Pages/                     # Page components
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
│   ├── App.razor                  # Root app component
│   ├── Routes.razor               # Routing configuration
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
│   ├── IOptimizationHubClient.cs
│   ├── JwtExtensions.cs
│   ├── OptimizationHubClient.cs
│   └── PlannerApiClient.cs
│
├── State/                         # State management with partial classes
│   ├── DispatchCenterState.cs
│   ├── DispatchCenterState.Customer.cs   # Partial class
│   ├── DispatchCenterState.Job.cs        # Partial class
│   ├── DispatchCenterState.Processing.cs # Partial class
│   ├── DispatchCenterState.Routes.cs     # Partial class
│   ├── DispatchCenterState.Tenant.cs     # Partial class
│   ├── DispatchCenterState.Vehicle.cs    # Partial class
│   │
│   └── Interfaces/                # State interfaces
│       ├── ICustomerState.cs
│       ├── IDispatchStateProcessing.cs
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
│   ├── app.css                   # Main application CSS
│   └── favicon.png               # Favicon
│
├── Program.cs                     # Application entry point
├── Planner.BlazorApp.csproj      # Project file
├── appsettings.json              # Configuration
└── appsettings.Development.json  # Development configuration
```

## Best Practices Implemented

### 1. Component Organization
- **Feature-based folders**: Components are grouped by feature (DispatchCenter/, WelcomeWizard/)
- **Shared components**: Reusable components are in Components/Shared/
- **Layout separation**: Layout components are in Components/Shared/Layout/

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
- **Interface pattern**: Services follow interface/implementation pattern (e.g., IOptimizationHubClient/OptimizationHubClient)
- **Clear naming**: Services use descriptive names indicating their purpose

### 5. Authentication
- **Dedicated folder**: Auth/ contains all authentication-related code
- **Interface pattern**: Follows interface/implementation pattern (IJwtTokenStore/JwtTokenStore)

### 6. Static Resources
- **Organized by type**: wwwroot/ contains only static files organized by type (css/, js/, images/, icons/)
- **No logic**: No code or logic files in wwwroot/
- **Asset organization**: Images are further organized (e.g., images/wizard/)

### 7. Naming Conventions
- **PascalCase**: All components, services, and interfaces use PascalCase
- **Descriptive names**: Names clearly indicate purpose and responsibility
- **Consistent suffixes**: Components end in component type (e.g., Modal, Tab, Panel)

## Migration Notes

### Recent Changes
- **Layout components moved**: MainLayout and NavMenu moved from Components/Layout/ to Components/Shared/Layout/
- **Namespace updated**: Layout components now use Planner.BlazorApp.Components.Shared.Layout namespace
- **Routes updated**: Routes.razor updated to reference new layout namespace
- **Project file fixed**: Removed exclusion rule that was blocking Components/Shared/ from compilation

### No Breaking Changes
- All application logic remains unchanged
- No functional changes to any components
- Build and tests continue to pass

## Future Considerations

1. Consider adding more granular shared components as needed (e.g., Components/Shared/Forms/, Components/Shared/Tables/)
2. If the application grows significantly, consider splitting features into separate feature folders with their own State and Services subfolders
3. Consider adding unit tests for each feature folder
4. Consider adding integration tests that verify the folder structure and naming conventions

## References

- [Blazor Project Structure Best Practices](https://learn.microsoft.com/en-us/aspnet/core/blazor/project-structure)
- [ASP.NET Core Blazor layouts](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/layouts)
