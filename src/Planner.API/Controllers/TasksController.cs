using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.Domain;
using Planner.Infrastructure.Persistence.Persistence;

namespace Planner.API.Controllers;

[Route("api/tasks")]
[Authorize]
public sealed class TasksController(PlannerDbContext db) : ControllerBase {
    [HttpGet]
    public async Task<ActionResult<List<TaskItem>>> GetAll() {
        var items = await db.Tasks
            .AsNoTracking()
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<TaskItem>> GetById(long id) {
        var entity = await db.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TaskItem entity) {
        db.Tasks.Add(entity);
        await db.SaveChangesAsync();
        return Created($"/api/tasks/{entity.Id}", entity);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] TaskItem updated) {
        var existing = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id);
        if (existing is null)
            return NotFound();

        db.Entry(existing).CurrentValues.SetValues(updated);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id) {
        var entity = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id);
        if (entity is null)
            return NotFound();

        db.Tasks.Remove(entity);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
