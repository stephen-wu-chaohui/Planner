using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.Domain;
using Planner.Infrastructure.Persistence;

namespace Planner.API.Controllers;

[Route("api/locations")]
[Authorize]
public sealed class LocationsController(PlannerDbContext db) : ControllerBase {
    [HttpGet]
    public async Task<ActionResult<List<Location>>> GetAll() {
        var items = await db.Locations
            .AsNoTracking()
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<Location>> GetById(long id) {
        var entity = await db.Locations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id);

        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Location entity) {
        db.Locations.Add(entity);
        await db.SaveChangesAsync();
        return Created($"/api/locations/{entity.Id}", entity);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] Location updated) {
        var existing = await db.Locations.FirstOrDefaultAsync(l => l.Id == id);
        if (existing is null)
            return NotFound();

        db.Entry(existing).CurrentValues.SetValues(updated);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id) {
        var entity = await db.Locations.FirstOrDefaultAsync(l => l.Id == id);
        if (entity is null)
            return NotFound();

        db.Locations.Remove(entity);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
