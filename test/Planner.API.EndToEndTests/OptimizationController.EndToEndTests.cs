using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Planner.API.Controllers;
using Planner.API.EndToEndTests.Fixtures;
using Planner.Application;
using Planner.Infrastructure.Persistence;
using Planner.Messaging.Messaging;
using Planner.Messaging.Optimization.Inputs;
using Planner.Messaging.Optimization.Outputs;
using Planner.Testing;
using Planner.Application.OptimizationRuns;
using Planner.Contracts.OptimizationRuns;
using Planner.Domain;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Planner.API.EndToEndTests;

public sealed class OptimizationControllerEndToEndTests {
    [Fact]
    public async Task Solve_endpoint_executes_full_pipeline_successfully() {
        using var factory = new TestApiFactory();

        var tenant = factory.Get<ITenantContext>();
        var db = factory.Get<PlannerDbContext>();
        var controller = factory.Get<OptimizationController>();
        // 1. MOCK THE AUTHORIZATION
        controller.MockUserContext();

        // 2. ENSURE SEEDING USES THE CORRECT TENANT ID
        // If this is empty, BuildRequestFromDomainAsync returns BadRequest
        DomainSeed.SeedSmallScenario(db, tenant.TenantId);
        await db.SaveChangesAsync(); // Ensure data is committed to the in-memory/test provider
        
        var result = await controller.Solve();

        // Act
        result.Should().NotBeNull();

        // Build request directly to validate solver invariants
        var method = controller
            .GetType()
            .GetMethod("BuildRequestFromDomainAsync",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

        method.Should().NotBeNull();

        var request = await (Task<OptimizeRouteRequest>) method!.Invoke(controller, [null])!;

        request.Should().NotBeNull();

        var bus = factory.Get<IMessageBus>() as FakeMessageBus;

        bus.Should().NotBeNull();
        bus!.PublishedMessages.Should().ContainSingle(m =>
            m.Route == MessageRoutes.Request);
    }

    [Fact]
    public async Task Solve_endpoint_in_azure_mode_creates_run_and_enqueues_lightweight_message() {
        using var factory = new TestApiFactory(dispatchMode: "AzureServiceBus");

        var tenant = factory.Get<ITenantContext>();
        var db = factory.Get<PlannerDbContext>();
        var controller = factory.Get<OptimizationController>();
        controller.MockUserContext();

        DomainSeed.SeedSmallScenario(db, tenant.TenantId);
        await db.SaveChangesAsync();

        var result = await controller.Solve();

        result.Should().BeOfType<OkObjectResult>();

        var store = (TestApiFactory.InMemoryOptimizationRunStore)factory.Get<IOptimizationRunStore>();
        var queue = (TestApiFactory.FakeOptimizationJobQueue)factory.Get<IOptimizationJobQueue>();
        var bus = factory.Get<IMessageBus>() as FakeMessageBus;

        store.Runs.Should().ContainSingle();
        queue.Messages.Should().ContainSingle();
        queue.Messages[0].TenantId.Should().Be(tenant.TenantId);
        queue.Messages[0].OptimizationRunId.Should().Be(store.Runs.Single().Key);
        bus!.PublishedMessages.Should().ContainSingle(m =>
            m.Route == MessageRoutes.Request);
    }

    [Fact]
    public async Task Solve_endpoint_in_azure_mode_snapshots_current_tenant_only() {
        using var factory = new TestApiFactory(dispatchMode: "AzureServiceBus");

        var tenant = factory.Get<ITenantContext>();
        var db = factory.Get<PlannerDbContext>();
        var controller = factory.Get<OptimizationController>();
        controller.MockUserContext();

        DomainSeed.SeedSmallScenario(db, tenant.TenantId);
        SeedOtherTenantSmallScenario(
            db,
            Guid.Parse("00000000-0000-0000-0000-000000000002"));
        await db.SaveChangesAsync();

        var result = await controller.Solve();

        result.Should().BeOfType<OkObjectResult>();

        var store = (TestApiFactory.InMemoryOptimizationRunStore)factory.Get<IOptimizationRunStore>();
        var run = store.Runs.Values.Single();

        run.TenantId.Should().Be(tenant.TenantId);
        run.RequestSnapshot.TenantId.Should().Be(tenant.TenantId);
        run.RequestSnapshot.Vehicles.Should().ContainSingle(v => v.VehicleId == 1);
        run.RequestSnapshot.Stops.Select(s => s.LocationId).Should().BeEquivalentTo([1L, 2L]);
    }

    [Fact]
    public async Task CompleteRun_endpoint_persists_solver_result_in_azure_mode() {
        using var factory = new TestApiFactory(dispatchMode: "AzureServiceBus");

        var tenant = factory.Get<ITenantContext>();
        var db = factory.Get<PlannerDbContext>();
        var controller = factory.Get<OptimizationController>();
        controller.MockUserContext();
        controller.Request.Headers["X-Optimization-Worker-Key"] = "test-worker-result-key";

        DomainSeed.SeedSmallScenario(db, tenant.TenantId);
        await db.SaveChangesAsync();

        var solveResult = await controller.Solve();
        solveResult.Should().BeOfType<OkObjectResult>();

        var store = (TestApiFactory.InMemoryOptimizationRunStore)factory.Get<IOptimizationRunStore>();
        var run = store.Runs.Values.Single();
        var response = new OptimizeRouteResponse(
            run.TenantId,
            run.OptimizationRunId,
            DateTime.UtcNow,
            [],
            0);

        var result = await controller.CompleteRun(run.OptimizationRunId, response, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        store.Runs[run.OptimizationRunId].SolverResult.Should().Be(response);
        store.Runs[run.OptimizationRunId].Status.Should().Be(OptimizationRunStatus.Succeeded);
    }

    private static void SeedOtherTenantSmallScenario(PlannerDbContext db, Guid tenantId) {
        var depotLocation = new Location {
            Id = 101,
            Address = "Other Tenant Depot",
            Latitude = -32.95,
            Longitude = 116.86
        };

        var depot = new Depot {
            Id = 101,
            TenantId = tenantId,
            Location = depotLocation,
            Name = "Other Hub"
        };

        var vehicle = new Vehicle {
            Id = 101,
            TenantId = tenantId,
            Name = "Other Truck",
            StartDepot = depot,
            EndDepot = depot,
            MaxPallets = 10,
            MaxWeight = 1000,
            ShiftLimitMinutes = 480,
            DriverRatePerHour = 50,
            MaintenanceRatePerHour = 10,
            FuelRatePerKm = 1.5,
            BaseFee = 20,
            SpeedFactor = 1.0
        };

        var customerLocation = new Location {
            Id = 102,
            Address = "Other Tenant Customer",
            Latitude = -32.96,
            Longitude = 116.87
        };

        var customer = new Customer {
            CustomerId = 101,
            TenantId = tenantId,
            Name = "Other Customer",
            Location = customerLocation,
            DefaultServiceMinutes = 15
        };

        var job = new Job {
            Id = 101,
            TenantId = tenantId,
            CustomerId = customer.CustomerId,
            Name = "Other Delivery",
            Location = customerLocation,
            JobType = JobType.Delivery,
            ServiceTimeMinutes = 15,
            ReadyTime = 0,
            DueTime = 1440,
            PalletDemand = 2,
            WeightDemand = 200
        };

        db.Locations.AddRange(depotLocation, customerLocation);
        db.Depots.Add(depot);
        db.Customers.Add(customer);
        db.Vehicles.Add(vehicle);
        db.Jobs.Add(job);
    }
}
