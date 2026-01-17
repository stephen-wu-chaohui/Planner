using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.API.Mappings;
using Planner.Application;
using Planner.Contracts.API;
using Planner.Domain;
using Planner.Infrastructure.Persistence;

namespace Planner.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ConfigController(PlannerDbContext db, ITenantContext tenant) : ControllerBase {

    [HttpGet("init")]
    public async Task<IActionResult> GetClientConfiguration() {
        var mainDepot = await db.Depots
            .AsNoTracking()
            .Include(d => d.Location)
            .FirstOrDefaultAsync();
        if (mainDepot == null) return NotFound("Main depot not found.");

        var tenantData = await db.Tenants
                    .Where(t => t.Id == tenant.TenantId)
                    .Select(t => new TenantInfo(t.Id, t.Name, mainDepot!.ToDto()))
                    .FirstOrDefaultAsync();

        if (tenantData == null) return NotFound("Tenant information not found.");

        return Ok(tenantData);
    }
}
