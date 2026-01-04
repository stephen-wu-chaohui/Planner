using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.Domain;
using Planner.Infrastructure.Persistence;

namespace Planner.API.Controllers;

[Route("api/vehicles")]
[Authorize]
public sealed class VehiclesController(PlannerDbContext db) : ControllerBase {
    [HttpGet]
    public async Task<ActionResult<List<Vehicle>>> GetAll() {
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

        return Ok(valid);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<Vehicle>> GetById(long id) {
        var entity = await db.Vehicles
            .AsNoTracking()
            .Include(v => v.StartDepot)
            .ThenInclude(d => d.Location)
            .Include(v => v.EndDepot)
            .ThenInclude(d => d.Location)
            .FirstOrDefaultAsync(v => v.Id == id);

        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Vehicle entity) {
        db.Vehicles.Add(entity);
        await db.SaveChangesAsync();
        return Created($"/api/vehicles/{entity.Id}", entity);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] Vehicle updated) {
        var existing = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == id);
        if (existing is null)
            return NotFound();

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
