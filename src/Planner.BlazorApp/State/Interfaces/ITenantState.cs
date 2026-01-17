using Planner.Contracts.API;

namespace Planner.BlazorApp.State.Interfaces;

/// <summary>
/// Interface for tenant-specific state management.
/// </summary>
public interface ITenantState : IDispatchStateProcessing {
    /// <summary>
    /// Retrieves and updates tenant metadata.
    /// </summary>
    Task LoadTenantInfo();

    /// <summary>
    /// Gets information about the current tenant, if available.
    /// </summary>
    TenantInfo? TenantInfo { get; }

    /// <summary>
    /// Event triggered when tenant metadata changes.
    /// </summary>
    event Action OnTenantInfoReady;
}
