using Microsoft.EntityFrameworkCore;
using Planner.Application;
using Planner.Infrastructure.Persistence;

namespace Planner.Infrastructure.Tests;

public class InfrastructureTests
{
    [Fact]
    public async Task DbContext_filters_entities_by_tenant() {
        var tenantId = Guid.NewGuid();

        var options = new DbContextOptionsBuilder<PlannerDbContext>()
            .UseInMemoryDatabase("planner-db")
            .Options;

        var tenantContext = Mock.Of<ITenantContext>(
            t => t.TenantId == tenantId
        );

        using var db = new PlannerDbContext(options, tenantContext);

        db.Vehicles.Add(new Vehicle { TenantId = tenantId, Name = "A" });
        db.Vehicles.Add(new Vehicle { TenantId = Guid.NewGuid(), Name = "B" });
        await db.SaveChangesAsync();

        var vehicles = await db.Vehicles.ToListAsync();

        vehicles.Should().HaveCount(1);
    }
}
