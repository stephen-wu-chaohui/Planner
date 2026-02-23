using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.Application;
using Planner.API.Mappings;
using Planner.Contracts.API;
using Planner.Infrastructure.Persistence;

namespace Planner.API.Controllers;

[Route("api/tenants")]
[Authorize]
public sealed class TenantsController(IPlannerDbContext db, ITenantContext tenant) : ControllerBase {
    /// <summary>
    /// Get tenant metadata including tenant name and main depot.
    /// </summary>
    /// <remarks>
    /// The main depot is determined by selecting the first depot associated with the tenant.
    /// In a future enhancement, this could be replaced with an explicit main depot designation.
    /// </remarks>
    [HttpGet("metadata")]
    public async Task<ActionResult<TenantDto>> GetMetadata() {
        var tenantEntity = await db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenant.TenantId);

        if (tenantEntity is null)
            return NotFound("Tenant not found.");

        // Find the first depot for this tenant to use as main depot
        var mainDepot = await db.Depots
            .AsNoTracking()
            .Where(d => d.TenantId == tenant.TenantId)
            .FirstOrDefaultAsync();

        return Ok(tenantEntity.ToDto(mainDepot?.Id));
    }
}
