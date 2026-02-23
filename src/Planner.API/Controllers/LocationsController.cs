using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.API.Mappings;
using Planner.Contracts.API;
using Planner.Infrastructure;

namespace Planner.API.Controllers;

[Route("api/locations")]
[Authorize]
public sealed class LocationsController(IPlannerDataCenter dataCenter) : ControllerBase {
    [HttpGet]
    public async Task<ActionResult<List<LocationDto>>> GetAll() {
        var items = await dataCenter.DbContext.Locations
            .AsNoTracking()
            .ToListAsync();

        return Ok(items.Select(l => l.ToDto()).ToList());
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<LocationDto>> GetById(long id) {
        var entity = await dataCenter.DbContext.Locations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id);

        return entity is null ? NotFound() : Ok(entity.ToDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LocationDto dto) {
        var entity = dto.ToDomain();
        dataCenter.DbContext.Locations.Add(entity);
        await dataCenter.DbContext.SaveChangesAsync();
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
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id) {
        var entity = await dataCenter.DbContext.Locations.FirstOrDefaultAsync(l => l.Id == id);
        if (entity is null)
            return NotFound();

        dataCenter.DbContext.Locations.Remove(entity);
        await dataCenter.DbContext.SaveChangesAsync();
        return NoContent();
    }
}
