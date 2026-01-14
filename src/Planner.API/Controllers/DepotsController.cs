using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.Application;
using Planner.API.Mappings;
using Planner.Contracts.API;
using Planner.Infrastructure.Persistence;

namespace Planner.API.Controllers;

[Route("api/depots")]
[Authorize]
public sealed class DepotsController(PlannerDbContext db, ITenantContext tenant) : ControllerBase {
    [HttpGet]
    public async Task<ActionResult<List<DepotDto>>> GetAll() {
        var items = await db.Depots
            .AsNoTracking()
            .Include(d => d.Location)
            .ToListAsync();

        return Ok(items.Select(d => d.ToDto()).ToList());
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<DepotDto>> GetById(long id) {
        var entity = await db.Depots
            .AsNoTracking()
            .Include(d => d.Location)
            .FirstOrDefaultAsync(d => d.Id == id);

        return entity is null ? NotFound() : Ok(entity.ToDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DepotDto dto) {
        var entity = dto.ToDomain(tenant.TenantId);
        db.Depots.Add(entity);
        await db.SaveChangesAsync();
        return Created($"/api/depots/{entity.Id}", entity.ToDto());
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] DepotDto dto) {
        var existing = await db.Depots.FirstOrDefaultAsync(d => d.Id == id);
        if (existing is null)
            return NotFound();

        var updated = dto.ToDomain(tenant.TenantId);
        db.Entry(existing).CurrentValues.SetValues(updated);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id) {
        var entity = await db.Depots.FirstOrDefaultAsync(d => d.Id == id);
        if (entity is null)
            return NotFound();

        db.Depots.Remove(entity);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
