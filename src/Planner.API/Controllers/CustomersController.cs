using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.Application;
using Planner.Contracts.API;
using Planner.API.Mappings;
using Planner.Infrastructure.Persistence;

namespace Planner.API.Controllers;

[Route("api/customers")]
[Authorize]
public sealed class CustomersController(PlannerDbContext db, ITenantContext tenant) : ControllerBase {

    [HttpGet]
    public async Task<ActionResult<List<CustomerDto>>> GetAll() {
        var items = await db.Customers
            .AsNoTracking()
            .Include(c => c.Location)
            .ToListAsync();

        return Ok(items.Select(c => c.ToDto()).ToList());
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CustomerDto>> GetById(long id) {
        var entity = await db.Customers
            .AsNoTracking()
            .Include(c => c.Location)
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        return entity is null ? NotFound() : Ok(entity.ToDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CustomerDto dto) {
        var entity = dto.ToDomain(tenant.TenantId);
        db.Customers.Add(entity);
        await db.SaveChangesAsync();
        return Created($"/api/customers/{entity.CustomerId}", entity.ToDto());
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] CustomerDto dto) {
        if (id != dto.CustomerId) {
            return BadRequest("ID mismatch");
        }

        var existing = db.Customers.Find(id);
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
        var entity = await db.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);
        if (entity is null)
            return NotFound();

        db.Customers.Remove(entity);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
