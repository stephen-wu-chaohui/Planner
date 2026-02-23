using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.Domain;
using Planner.Infrastructure;
using Route = Planner.Domain.Route;

namespace Planner.API.Controllers;

[Route("api/routes")]
[Authorize]
public sealed class RoutesController(IPlannerDataCenter dataCenter) : ControllerBase {
    [HttpGet]
    public async Task<ActionResult<List<Route>>> GetAll() {
        var items = await dataCenter.DbContext.Set<Route>()
            .AsNoTracking()
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<Route>> GetById(long id) {
        var entity = await dataCenter.DbContext.Set<Route>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Route entity) {
        dataCenter.DbContext.Set<Route>().Add(entity);
        await dataCenter.DbContext.SaveChangesAsync();
        return Created($"/api/routes/{entity.Id}", entity);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] Route updated) {
        var existing = await dataCenter.DbContext.Set<Route>().FirstOrDefaultAsync(r => r.Id == id);
        if (existing is null)
            return NotFound();

        dataCenter.DbContext.Entry(existing).CurrentValues.SetValues(updated);
        await dataCenter.DbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id) {
        var entity = await dataCenter.DbContext.Set<Route>().FirstOrDefaultAsync(r => r.Id == id);
        if (entity is null)
            return NotFound();

        dataCenter.DbContext.Set<Route>().Remove(entity);
        await dataCenter.DbContext.SaveChangesAsync();
        return NoContent();
    }
}
