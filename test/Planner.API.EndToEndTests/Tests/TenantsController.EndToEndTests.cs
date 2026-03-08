using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Planner.API.Controllers;
using Planner.API.EndToEndTests.Fixtures;
using Planner.Application;
using Planner.Contracts.API;
using Planner.Domain;
using Planner.Infrastructure;
using Planner.Infrastructure.Persistence;
using Planner.Testing;
using System.Threading.Tasks;
using Xunit;

namespace Planner.API.EndToEndTests.Tests;

public sealed class TenantsControllerEndToEndTests {
    [Fact]
    public async Task GetMetadata_returns_tenant_with_main_depot() {
        // Arrange
        using var factory = new TestApiFactory();
        var tenant = factory.Get<ITenantContext>();
        var db = factory.Get<PlannerDbContext>();
        var dataCenter = factory.Get<IPlannerDataCenter>();
        
        // Seed test data
        var testTenant = new Tenant { Id = tenant.TenantId, Name = "Test Tenant" };
        var testLocation = new Location(1, "123 Main St", 40.7128, -74.0060);
        var testDepot = new Depot { 
            Id = 1, 
            TenantId = tenant.TenantId, 
            Name = "Main Depot",
            LocationId = testLocation.Id,
            Location = testLocation
        };
        
        db.Tenants.Add(testTenant);
        db.Locations.Add(testLocation);
        db.Depots.Add(testDepot);
        await db.SaveChangesAsync();

        // Create controller with the same DataCenter
        var controller = new TenantsController(dataCenter, tenant);
        controller.MockUserContext();

        // Act
        var result = await controller.GetMetadata();

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var tenantDto = okResult.Value.Should().BeOfType<TenantDto>().Subject;
        
        tenantDto.Id.Should().Be(tenant.TenantId);
        tenantDto.Name.Should().Be("Test Tenant");
        tenantDto.MainDepotId.Should().Be(1);
    }

    [Fact]
    public async Task GetMetadata_returns_not_found_when_tenant_does_not_exist() {
        // Arrange
        using var factory = new TestApiFactory();
        var tenant = factory.Get<ITenantContext>();
        var dataCenter = factory.Get<IPlannerDataCenter>();

        // Create controller without seeding tenant
        var controller = new TenantsController(dataCenter, tenant);
        controller.MockUserContext();

        // Act
        var result = await controller.GetMetadata();

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }
}
