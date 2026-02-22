using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.Domain;
using Planner.Infrastructure.Persistence;
using Route = Planner.Domain.Route;

namespace Planner.API.Controllers;

[Route("api/routes")]
[Authorize]
public sealed class RoutesController(IPlannerDbContext db) : ControllerBase {
    [HttpGet]
    public async Task<ActionResult<List<Route>>> GetAll() {
        var items = await db.Set<Route>()
            .AsNoTracking()
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<Route>> GetById(long id) {
        var entity = await db.Set<Route>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Route entity) {
        db.Set<Route>().Add(entity);
        await db.SaveChangesAsync();
        return Created($"/api/routes/{entity.Id}", entity);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] Route updated) {
        var existing = await db.Set<Route>().FirstOrDefaultAsync(r => r.Id == id);
        if (existing is null)
            return NotFound();

        db.Entry(existing).CurrentValues.SetValues(updated);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id) {
        var entity = await db.Set<Route>().FirstOrDefaultAsync(r => r.Id == id);
        if (entity is null)
            return NotFound();

        db.Set<Route>().Remove(entity);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
