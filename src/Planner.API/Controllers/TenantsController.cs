using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.Application;
using Planner.API.Caching;
using Planner.API.Mappings;
using Planner.Contracts.API;
using Planner.Infrastructure;

namespace Planner.API.Controllers;

[Route("api/tenants")]
[Authorize]
public sealed class TenantsController(IPlannerDataCenter dataCenter, ITenantContext tenant) : ControllerBase {
    /// <summary>
    /// Get tenant metadata including tenant name and main depot.
    /// </summary>
    /// <remarks>
    /// The main depot is determined by selecting the first depot associated with the tenant.
    /// In a future enhancement, this could be replaced with an explicit main depot designation.
    /// </remarks>
    [HttpGet("metadata")]
    public async Task<ActionResult<TenantDto>> GetMetadata() {
        var metadata = await dataCenter.GetOrFetchAsync(
            CacheKeys.TenantMetadata(tenant.TenantId),
            async () => {
                var tenantEntity = await dataCenter.DbContext.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == tenant.TenantId);

                if (tenantEntity is null) {
                    return null;
                }

                var mainDepot = await dataCenter.DbContext.Depots
                    .AsNoTracking()
                    .Where(d => d.TenantId == tenant.TenantId)
                    .FirstOrDefaultAsync();

                return tenantEntity.ToDto(mainDepot?.Id);
            });

        if (metadata is null)
            return NotFound("Tenant not found.");

        return Ok(metadata);
    }
}
