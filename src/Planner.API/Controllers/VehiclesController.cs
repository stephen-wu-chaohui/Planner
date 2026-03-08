using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.Application;
using Planner.API.Caching;
using Planner.API.Mappings;
using Planner.Contracts.API;
using Planner.Infrastructure;

namespace Planner.API.Controllers;

[Route("api/vehicles")]
[Authorize]
public sealed class VehiclesController(IPlannerDataCenter dataCenter, ITenantContext tenant) : ControllerBase {
    [HttpGet]
    public async Task<ActionResult<List<VehicleDto>>> GetAll() {
        var payload = await dataCenter.GetOrFetchAsync(
            CacheKeys.VehiclesList(tenant.TenantId),
            async () => {
                var items = await dataCenter.DbContext.Vehicles
                    .AsNoTracking()
                    .Include(v => v.StartDepot)
                    .ThenInclude(d => d.Location)
                    .Include(v => v.EndDepot)
                    .ThenInclude(d => d.Location)
                    .ToListAsync();

                var valid = items
                    .Where(v =>
                        v.StartDepot is not null &&
                        v.EndDepot is not null &&
                        v.StartDepot.Location is not null &&
                        v.EndDepot.Location is not null)
                    .Select(v => v.ToDto())
                    .ToList();

                return new VehicleListPayload(valid, items.Count - valid.Count);
            });

        var result = payload ?? new VehicleListPayload([], 0);

        if (result.OmittedCount > 0) {
            Response.Headers.Append(
                "X-Warning",
                $"{result.OmittedCount} vehicle(s) omitted due to missing StartDepot/EndDepot navigation.");
        }

        return Ok(result.Items);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<VehicleDto>> GetById(long id) {
        var entity = await dataCenter.GetOrFetchAsync(
            CacheKeys.VehicleById(id, tenant.TenantId),
            async () => await dataCenter.DbContext.Vehicles
                .AsNoTracking()
                .Include(v => v.StartDepot)
                .ThenInclude(d => d.Location)
                .Include(v => v.EndDepot)
                .ThenInclude(d => d.Location)
                .Where(v =>
                    v.Id == id &&
                    v.StartDepot != null &&
                    v.EndDepot != null &&
                    v.StartDepot.Location != null &&
                    v.EndDepot.Location != null)
                .Select(v => v.ToDto())
                .FirstOrDefaultAsync());

        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VehicleDto dto) {
        var entity = dto.ToDomain(tenant.TenantId);
        dataCenter.DbContext.Vehicles.Add(entity);
        await dataCenter.DbContext.SaveChangesAsync();
        await dataCenter.RemoveCacheKeysAsync(
            HttpContext.RequestAborted,
            CacheKeys.VehiclesList(tenant.TenantId),
            CacheKeys.VehicleById(entity.Id, tenant.TenantId));
        return Created($"/api/vehicles/{entity.Id}", entity.ToDto());
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] VehicleDto dto) {
        if (id != dto.Id) {
            return BadRequest("ID mismatch");
        }

        var existing = await dataCenter.DbContext.Vehicles.FindAsync(id);
        if (existing == null) {
            return NotFound();
        }

        // Map DTO to Entity (Full replacement)
        var updated = dto.ToDomain(tenant.TenantId);
        dataCenter.DbContext.Entry(existing).CurrentValues.SetValues(updated);
        await dataCenter.DbContext.SaveChangesAsync();
        await dataCenter.RemoveCacheKeysAsync(
            HttpContext.RequestAborted,
            CacheKeys.VehiclesList(tenant.TenantId),
            CacheKeys.VehicleById(id, tenant.TenantId));

        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id) {
        var entity = await dataCenter.DbContext.Vehicles.FirstOrDefaultAsync(v => v.Id == id);
        if (entity is null)
            return NotFound();

        dataCenter.DbContext.Vehicles.Remove(entity);
        await dataCenter.DbContext.SaveChangesAsync();
        await dataCenter.RemoveCacheKeysAsync(
            HttpContext.RequestAborted,
            CacheKeys.VehiclesList(tenant.TenantId),
            CacheKeys.VehicleById(id, tenant.TenantId));
        return NoContent();
    }

    private sealed record VehicleListPayload(List<VehicleDto> Items, int OmittedCount);
}
