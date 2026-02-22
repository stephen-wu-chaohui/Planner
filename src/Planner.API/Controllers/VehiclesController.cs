using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.Application;
using Planner.API.Mappings;
using Planner.Contracts.API;
using Planner.Infrastructure.Persistence;

namespace Planner.API.Controllers;

[Route("api/vehicles")]
[Authorize]
public sealed class VehiclesController(IPlannerDbContext db, ITenantContext tenant) : ControllerBase {
    [HttpGet]
    public async Task<ActionResult<List<VehicleDto>>> GetAll() {
        var items = await db.Vehicles
            .AsNoTracking()
            .Include(v => v.StartDepot)
            .ThenInclude(d => d.Location)
            .Include(v => v.EndDepot)
            .ThenInclude(d => d.Location)
            .ToListAsync();

        var valid = items
            .Where(v => v.StartDepot is not null && v.EndDepot is not null)
            .ToList();

        if (valid.Count != items.Count) {
            Response.Headers.Append(
                "X-Warning",
                $"{items.Count - valid.Count} vehicle(s) omitted due to missing StartDepot/EndDepot navigation.");
        }

        return Ok(valid.Select(v => v.ToDto()).ToList());
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<VehicleDto>> GetById(long id) {
        var entity = await db.Vehicles
            .AsNoTracking()
            .Include(v => v.StartDepot)
            .ThenInclude(d => d.Location)
            .Include(v => v.EndDepot)
            .ThenInclude(d => d.Location)
            .FirstOrDefaultAsync(v => v.Id == id);

        return entity is null ? NotFound() : Ok(entity.ToDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VehicleDto dto) {
        var entity = dto.ToDomain(tenant.TenantId);
        db.Vehicles.Add(entity);
        await db.SaveChangesAsync();
        return Created($"/api/vehicles/{entity.Id}", entity.ToDto());
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] VehicleDto dto) {
        if (id != dto.Id) {
            return BadRequest("ID mismatch");
        }

        var existing = await db.Vehicles.FindAsync(id);
        if (existing == null) {
            return NotFound();
        }

        // Map DTO to Entity (Full replacement)
        var updated = dto.ToDomain(tenant.TenantId);
        db.Entry(existing).CurrentValues.SetValues(updated);
        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id) {
        var entity = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == id);
        if (entity is null)
            return NotFound();

        db.Vehicles.Remove(entity);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
