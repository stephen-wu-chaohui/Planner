namespace Planner.Contracts.API;

/// <summary>
/// The information about a tenant.
/// </summary>
/// <param name="TenantId"></param>
/// <param name="TenantName"></param>
/// <param name="MainDepot"></param>
public record TenantInfo(Guid TenantId, string TenantName, DepotDto MainDepot);
