using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.Application;
using Planner.API.Caching;
using Planner.API.Mappings;
using Planner.Contracts.API;
using Planner.Infrastructure;

namespace Planner.API.Controllers;

[Route("api/jobs")]
[Authorize]
public sealed class JobsController(IPlannerDataCenter dataCenter, ITenantContext tenant) : ControllerBase {
    [HttpGet]
    public async Task<ActionResult<List<JobDto>>> GetAll() {
        var items = await dataCenter.GetOrFetchAsync(
            CacheKeys.JobsList(tenant.TenantId),
            async () => await dataCenter.DbContext.Jobs
                .AsNoTracking()
                .Include(j => j.Location)
                .Select(j => j.ToDto())
                .ToListAsync());

        return Ok(items ?? []);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<JobDto>> GetById(long id) {
        var entity = await dataCenter.GetOrFetchAsync(
            CacheKeys.JobById(id, tenant.TenantId),
            async () => await dataCenter.DbContext.Jobs
                .AsNoTracking()
                .Include(j => j.Location)
                .Where(j => j.Id == id)
                .Select(j => j.ToDto())
                .FirstOrDefaultAsync());

        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] JobDto dto) {
        var entity = dto.ToDomain(tenant.TenantId);
        dataCenter.DbContext.Jobs.Add(entity);
        await dataCenter.DbContext.SaveChangesAsync();
        await dataCenter.RemoveCacheKeysAsync(
            HttpContext.RequestAborted,
            CacheKeys.JobsList(tenant.TenantId),
            CacheKeys.JobById(entity.Id, tenant.TenantId));
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
        await dataCenter.RemoveCacheKeysAsync(
            HttpContext.RequestAborted,
            CacheKeys.JobsList(tenant.TenantId),
            CacheKeys.JobById(id, tenant.TenantId));

        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id) {
        var entity = await dataCenter.DbContext.Jobs.FirstOrDefaultAsync(j => j.Id == id);
        if (entity is null)
            return NotFound();

        dataCenter.DbContext.Jobs.Remove(entity);
        await dataCenter.DbContext.SaveChangesAsync();
        await dataCenter.RemoveCacheKeysAsync(
            HttpContext.RequestAborted,
            CacheKeys.JobsList(tenant.TenantId),
            CacheKeys.JobById(id, tenant.TenantId));
        return NoContent();
    }
}
