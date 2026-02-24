using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.Domain;
using Planner.Infrastructure;

namespace Planner.API.Controllers;

[Route("api/tasks")]
[Authorize]
public sealed class TasksController(IPlannerDataCenter dataCenter) : ControllerBase {
    [HttpGet]
    public async Task<ActionResult<List<TaskItem>>> GetAll() {
        var items = await dataCenter.DbContext.Tasks
            .AsNoTracking()
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<TaskItem>> GetById(long id) {
        var entity = await dataCenter.DbContext.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TaskItem entity) {
        dataCenter.DbContext.Tasks.Add(entity);
        await dataCenter.DbContext.SaveChangesAsync();
        return Created($"/api/tasks/{entity.Id}", entity);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] TaskItem updated) {
        var existing = await dataCenter.DbContext.Tasks.FirstOrDefaultAsync(t => t.Id == id);
        if (existing is null)
            return NotFound();

        dataCenter.DbContext.Entry(existing).CurrentValues.SetValues(updated);
        await dataCenter.DbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id) {
        var entity = await dataCenter.DbContext.Tasks.FirstOrDefaultAsync(t => t.Id == id);
        if (entity is null)
            return NotFound();

        dataCenter.DbContext.Tasks.Remove(entity);
        await dataCenter.DbContext.SaveChangesAsync();
        return NoContent();
    }
}
