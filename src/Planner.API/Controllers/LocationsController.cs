using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.API.Caching;
using Planner.API.Mappings;
using Planner.Contracts.API;
using Planner.Infrastructure;

namespace Planner.API.Controllers;

[Route("api/locations")]
[Authorize]
public sealed class LocationsController(IPlannerDataCenter dataCenter) : ControllerBase {
    [HttpGet]
    public async Task<ActionResult<List<LocationDto>>> GetAll() {
        var items = await dataCenter.GetOrFetchAsync(
            CacheKeys.LocationsList(),
            async () => await dataCenter.DbContext.Locations
                .AsNoTracking()
                .Select(l => l.ToDto())
                .ToListAsync());

        return Ok(items ?? []);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<LocationDto>> GetById(long id) {
        var entity = await dataCenter.GetOrFetchAsync(
            CacheKeys.LocationById(id),
            async () => await dataCenter.DbContext.Locations
                .AsNoTracking()
                .Where(l => l.Id == id)
                .Select(l => l.ToDto())
                .FirstOrDefaultAsync());

        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LocationDto dto) {
        var entity = dto.ToDomain();
        dataCenter.DbContext.Locations.Add(entity);
        await dataCenter.DbContext.SaveChangesAsync();
        await dataCenter.RemoveCacheKeysAsync(
            HttpContext.RequestAborted,
            CacheKeys.LocationsList(),
            CacheKeys.LocationById(entity.Id));
        return Created($"/api/locations/{entity.Id}", entity.ToDto());
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] LocationDto dto) {
        var existing = await dataCenter.DbContext.Locations.FirstOrDefaultAsync(l => l.Id == id);
        if (existing is null)
            return NotFound();

        var updated = dto.ToDomain();
        dataCenter.DbContext.Entry(existing).CurrentValues.SetValues(updated);
        await dataCenter.DbContext.SaveChangesAsync();
        await dataCenter.RemoveCacheKeysAsync(
            HttpContext.RequestAborted,
            CacheKeys.LocationsList(),
            CacheKeys.LocationById(id));
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id) {
        var entity = await dataCenter.DbContext.Locations.FirstOrDefaultAsync(l => l.Id == id);
        if (entity is null)
            return NotFound();

        dataCenter.DbContext.Locations.Remove(entity);
        await dataCenter.DbContext.SaveChangesAsync();
        await dataCenter.RemoveCacheKeysAsync(
            HttpContext.RequestAborted,
            CacheKeys.LocationsList(),
            CacheKeys.LocationById(id));
        return NoContent();
    }
}
