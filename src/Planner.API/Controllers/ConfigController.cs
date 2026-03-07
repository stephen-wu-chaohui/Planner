using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.API.Caching;
using Planner.API.Mappings;
using Planner.Application;
using Planner.Contracts.API;
using Planner.Infrastructure;

namespace Planner.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ConfigController(IPlannerDataCenter dataCenter, ITenantContext tenant) : ControllerBase {

    [HttpGet("init")]
    public async Task<IActionResult> GetClientConfiguration() {
        var tenantData = await dataCenter.GetOrFetchAsync(
            CacheKeys.ConfigInit(tenant.TenantId),
            async () => {
                var mainDepot = await dataCenter.DbContext.Depots
                    .AsNoTracking()
                    .Include(d => d.Location)
                    .FirstOrDefaultAsync();

                if (mainDepot is null) {
                    return null;
                }

                return await dataCenter.DbContext.Tenants
                    .Where(t => t.Id == tenant.TenantId)
                    .Select(t => new TenantInfo(t.Id, t.Name, mainDepot.ToDto()))
                    .FirstOrDefaultAsync();
            });

        if (tenantData == null) return NotFound("Tenant information not found.");

        return Ok(tenantData);
    }
}
