using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.Domain;
using Planner.Infrastructure.Persistence;

namespace Planner.API.Controllers;

[Route("api/jobs")]
[Authorize]
public sealed class JobsController(PlannerDbContext db) : ControllerBase {
    [HttpGet]
    public async Task<ActionResult<List<Job>>> GetAll() {
        var items = await db.Jobs
            .AsNoTracking()
            .Include(j => j.Location)
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<Job>> GetById(long id) {
        var entity = await db.Jobs
            .AsNoTracking()
            .Include(j => j.Location)
            .FirstOrDefaultAsync(j => j.Id == id);

        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Job entity) {
        db.Jobs.Add(entity);
        await db.SaveChangesAsync();
        return Created($"/api/jobs/{entity.Id}", entity);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] Job updated) {
        var existing = await db.Jobs.FirstOrDefaultAsync(j => j.Id == id);
        if (existing is null)
            return NotFound();

        db.Entry(existing).CurrentValues.SetValues(updated);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id) {
        var entity = await db.Jobs.FirstOrDefaultAsync(j => j.Id == id);
        if (entity is null)
            return NotFound();

        db.Jobs.Remove(entity);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
