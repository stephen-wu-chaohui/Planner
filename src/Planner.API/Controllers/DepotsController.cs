using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.Domain;
using Planner.Infrastructure.Persistence;

namespace Planner.API.Controllers;

[Route("api/depots")]
[Authorize]
public sealed class DepotsController(PlannerDbContext db) : ControllerBase {
    [HttpGet]
    public async Task<ActionResult<List<Depot>>> GetAll() {
        var items = await db.Depots
            .AsNoTracking()
            .Include(d => d.Location)
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<Depot>> GetById(long id) {
        var entity = await db.Depots
            .AsNoTracking()
            .Include(d => d.Location)
            .FirstOrDefaultAsync(d => d.Id == id);

        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Depot entity) {
        db.Depots.Add(entity);
        await db.SaveChangesAsync();
        return Created($"/api/depots/{entity.Id}", entity);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] Depot updated) {
        var existing = await db.Depots.FirstOrDefaultAsync(d => d.Id == id);
        if (existing is null)
            return NotFound();

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
