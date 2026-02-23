using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.Application;
using Planner.API.Mappings;
using Planner.Contracts.API;
using Planner.Infrastructure;

namespace Planner.API.Controllers;

[Route("api/jobs")]
[Authorize]
public sealed class JobsController(IPlannerDataCenter dataCenter, ITenantContext tenant) : ControllerBase {
    [HttpGet]
    public async Task<ActionResult<List<JobDto>>> GetAll() {
        var items = await dataCenter.DbContext.Jobs
            .AsNoTracking()
            .Include(j => j.Location)
            .ToListAsync();

        return Ok(items.Select(j => j.ToDto()).ToList());
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<JobDto>> GetById(long id) {
        var entity = await dataCenter.DbContext.Jobs
            .AsNoTracking()
            .Include(j => j.Location)
            .FirstOrDefaultAsync(j => j.Id == id);

        return entity is null ? NotFound() : Ok(entity.ToDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] JobDto dto) {
        var entity = dto.ToDomain(tenant.TenantId);
        dataCenter.DbContext.Jobs.Add(entity);
        await dataCenter.DbContext.SaveChangesAsync();
        return Created($"/api/jobs/{entity.Id}", entity.ToDto());
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] JobDto dto) {
        if (id != dto.Id) {
            return BadRequest("ID mismatch");
        }

        var existing = await dataCenter.DbContext.Jobs.FindAsync(id);
        if (existing == null) {
            return NotFound();
        }

        // Map DTO to Entity (Full replacement)
        var updated = dto.ToDomain(tenant.TenantId);
        dataCenter.DbContext.Entry(existing).CurrentValues.SetValues(updated);
        await dataCenter.DbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id) {
        var entity = await dataCenter.DbContext.Jobs.FirstOrDefaultAsync(j => j.Id == id);
        if (entity is null)
            return NotFound();

        dataCenter.DbContext.Jobs.Remove(entity);
        await dataCenter.DbContext.SaveChangesAsync();
        return NoContent();
    }
}
