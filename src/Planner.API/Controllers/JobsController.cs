using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.Application;
using Planner.API.Mappings;
using Planner.Contracts.API;
using Planner.Infrastructure.Persistence;

namespace Planner.API.Controllers;

[Route("api/jobs")]
[Authorize]
public sealed class JobsController(PlannerDbContext db, ITenantContext tenant) : ControllerBase {
    [HttpGet]
    public async Task<ActionResult<List<JobDto>>> GetAll() {
        var items = await db.Jobs
            .AsNoTracking()
            .Include(j => j.Location)
            .ToListAsync();

        return Ok(items.Select(j => j.ToDto()).ToList());
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<JobDto>> GetById(long id) {
        var entity = await db.Jobs
            .AsNoTracking()
            .Include(j => j.Location)
            .FirstOrDefaultAsync(j => j.Id == id);

        return entity is null ? NotFound() : Ok(entity.ToDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] JobDto dto) {
        var entity = dto.ToDomain(tenant.TenantId);
        db.Jobs.Add(entity);
        await db.SaveChangesAsync();
        return Created($"/api/jobs/{entity.Id}", entity.ToDto());
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] JobDto dto) {
        if (id != dto.Id) {
            return BadRequest("ID mismatch");
        }

        var existing = db.Jobs.Find(id);
        if (existing == null) {
            return NotFound();
        }

        // Map DTO to Entity (Full replacement)
        var updated = dto.ToDomain(tenant.TenantId);
        db.Entry(existing).CurrentValues.SetValues(updated);
        db.SaveChanges();

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
