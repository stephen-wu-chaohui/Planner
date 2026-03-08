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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Planner.API.EndToEndTests.Tests;

public sealed class CustomersControllerCacheAsideEndToEndTests {
    [Fact]
    public async Task GetById_returns_cached_value_after_entity_removed_from_db() {
        using var factory = new TestApiFactory();
        var tenant = factory.Get<ITenantContext>();
        var db = factory.Get<PlannerDbContext>();
        var dataCenter = factory.Get<IPlannerDataCenter>();

        SeedCustomer(db, tenant.TenantId, id: 1, name: "Alpha");

        var controller = new CustomersController(dataCenter, tenant);
        controller.MockUserContext();

        var first = await controller.GetById(1);
        var firstDto = first.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<CustomerDto>().Subject;
        firstDto.Name.Should().Be("Alpha");

        var entity = await db.Customers.FindAsync(1L);
        db.Customers.Remove(entity!);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var second = await controller.GetById(1);
        var secondDto = second.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<CustomerDto>().Subject;
        secondDto.CustomerId.Should().Be(1);
        secondDto.Name.Should().Be("Alpha");
    }

    [Fact]
    public async Task Update_invalidates_cached_item_and_next_get_returns_fresh_value() {
        using var factory = new TestApiFactory();
        var tenant = factory.Get<ITenantContext>();
        var db = factory.Get<PlannerDbContext>();
        var dataCenter = factory.Get<IPlannerDataCenter>();

        SeedCustomer(db, tenant.TenantId, id: 1, name: "Before");

        var controller = new CustomersController(dataCenter, tenant);
        controller.MockUserContext();

        var cachedRead = await controller.GetById(1);
        cachedRead.Result.Should().BeOfType<OkObjectResult>();

        var updateDto = new CustomerDto(
            CustomerId: 1,
            Name: "After",
            Location: new LocationDto(1001, "1 Cache Lane", -31.95, 115.86),
            DefaultServiceMinutes: 30,
            RequiresRefrigeration: false);

        var updateResult = await controller.Update(1, updateDto);
        updateResult.Should().BeOfType<NoContentResult>();

        var refreshed = await controller.GetById(1);
        var refreshedDto = refreshed.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<CustomerDto>().Subject;
        refreshedDto.Name.Should().Be("After");
    }

    [Fact]
    public async Task Create_invalidates_cached_list_and_next_getall_reflects_new_row() {
        using var factory = new TestApiFactory();
        var tenant = factory.Get<ITenantContext>();
        var db = factory.Get<PlannerDbContext>();
        var dataCenter = factory.Get<IPlannerDataCenter>();

        SeedCustomer(db, tenant.TenantId, id: 1, name: "Existing");

        var controller = new CustomersController(dataCenter, tenant);
        controller.MockUserContext();

        var first = await controller.GetAll();
        var firstList = first.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<List<CustomerDto>>().Subject;
        firstList.Should().HaveCount(1);

        var createDto = new CustomerDto(
            CustomerId: 2,
            Name: "New Customer",
            Location: new LocationDto(1002, "2 Invalidate Ave", -31.94, 115.87),
            DefaultServiceMinutes: 20,
            RequiresRefrigeration: false);

        var createResult = await controller.Create(createDto);
        createResult.Should().BeOfType<CreatedResult>();

        var second = await controller.GetAll();
        var secondList = second.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<List<CustomerDto>>().Subject;
        secondList.Should().HaveCount(2);
        secondList.Select(c => c.Name).Should().Contain(["Existing", "New Customer"]);
    }

    private static void SeedCustomer(PlannerDbContext db, Guid tenantId, long id, string name) {
        var location = new Location {
            Id = 1000 + id,
            Address = $"{id} Seed Street",
            Latitude = -31.95 + (id * 0.001),
            Longitude = 115.86 + (id * 0.001)
        };

        var customer = new Customer {
            CustomerId = id,
            TenantId = tenantId,
            Name = name,
            LocationId = location.Id,
            Location = location,
            DefaultServiceMinutes = 15,
            RequiresRefrigeration = false
        };

        db.Locations.Add(location);
        db.Customers.Add(customer);
        db.SaveChanges();
    }
}
