namespace Planner.Contracts.API;

/// <summary>
/// Tenant metadata representation for API contracts.
/// </summary>
/// <param name="Id">Tenant identifier.</param>
/// <param name="Name">Tenant display name.</param>
/// <param name="MainDepotId">Main depot identifier for the tenant.</param>
public sealed record TenantDto(
    Guid Id,
    string Name,
    long? MainDepotId);
