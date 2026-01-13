using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.Application;
using Planner.Domain;
using Planner.Infrastructure.Persistence;
using System.Reflection;

namespace Planner.API.Controllers;

[Route("api/customers")]
[Authorize]
public sealed class CustomersController(PlannerDbContext db, ITenantContext tenant) : ControllerBase {
    private static readonly PropertyInfo? CustomerTenantIdProperty =
        typeof(Customer).GetProperty(nameof(Customer.TenantId));

    private static void ForceTenant(Customer entity, Guid tenantId) {
        CustomerTenantIdProperty?.SetValue(entity, tenantId);
    }

    [HttpGet]
    public async Task<ActionResult<List<Customer>>> GetAll() {
        var items = await db.Customers
            .AsNoTracking()
            .Include(c => c.Location)
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<Customer>> GetById(long id) {
        var entity = await db.Customers
            .AsNoTracking()
            .Include(c => c.Location)
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Customer entity) {
        ForceTenant(entity, tenant.TenantId);
        db.Customers.Add(entity);
        await db.SaveChangesAsync();
        return Created($"/api/customers/{entity.CustomerId}", entity);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] Customer updated) {
        var existing = await db.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);
        if (existing is null)
            return NotFound();

        ForceTenant(updated, tenant.TenantId);
        db.Entry(existing).CurrentValues.SetValues(updated);
        await db.SaveChangesAsync();
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
