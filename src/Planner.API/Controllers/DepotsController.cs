using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.Application;
using Planner.API.Mappings;
using Planner.Contracts.API;
using Planner.Infrastructure;

namespace Planner.API.Controllers;

[Route("api/depots")]
[Authorize]
public sealed class DepotsController(IPlannerDataCenter dataCenter, ITenantContext tenant) : ControllerBase {
    [HttpGet]
    public async Task<ActionResult<List<DepotDto>>> GetAll() {
        var items = await dataCenter.DbContext.Depots
            .AsNoTracking()
            .Include(d => d.Location)
            .ToListAsync();

        return Ok(items.Select(d => d.ToDto()).ToList());
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<DepotDto>> GetById(long id) {
        var entity = await dataCenter.DbContext.Depots
            .AsNoTracking()
            .Include(d => d.Location)
            .FirstOrDefaultAsync(d => d.Id == id);

        return entity is null ? NotFound() : Ok(entity.ToDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DepotDto dto) {
        var entity = dto.ToDomain(tenant.TenantId);
        dataCenter.DbContext.Depots.Add(entity);
        await dataCenter.DbContext.SaveChangesAsync();
        return Created($"/api/depots/{entity.Id}", entity.ToDto());
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] DepotDto dto) {
        var existing = await dataCenter.DbContext.Depots.FirstOrDefaultAsync(d => d.Id == id);
        if (existing is null)
            return NotFound();

        var updated = dto.ToDomain(tenant.TenantId);
        dataCenter.DbContext.Entry(existing).CurrentValues.SetValues(updated);
        await dataCenter.DbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id) {
        var entity = await dataCenter.DbContext.Depots.FirstOrDefaultAsync(d => d.Id == id);
        if (entity is null)
            return NotFound();

        dataCenter.DbContext.Depots.Remove(entity);
        await dataCenter.DbContext.SaveChangesAsync();
        return NoContent();
    }
}
