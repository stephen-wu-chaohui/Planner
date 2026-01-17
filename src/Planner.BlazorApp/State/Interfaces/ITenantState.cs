using Planner.Contracts.API;

namespace Planner.BlazorApp.State.Interfaces;

/// <summary>
/// Interface for tenant-specific state management.
/// </summary>
public interface ITenantState : IDispatchStateProcessing {
    /// <summary>
    /// Gets the current tenant metadata.
    /// </summary>
    TenantDto? Tenant { get; }

    /// <summary>
    /// Event triggered when tenant metadata changes.
    /// </summary>
    event Action OnTenantChanged;

    /// <summary>
    /// Retrieves and updates tenant metadata.
    /// </summary>
    Task LoadTenantMetadataAsync();
}
