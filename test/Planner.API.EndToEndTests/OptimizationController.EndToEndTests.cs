using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Planner.API.Controllers;
using Planner.API.EndToEndTests.Fixtures;
using Planner.Application;
using Planner.Infrastructure.Persistence;
using Planner.Messaging.Messaging;
using Planner.Messaging.Optimization.Inputs;
using Planner.Testing;
using Planner.Application.OptimizationRuns;
using System.Linq;
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
        bus!.PublishedMessages.Should().BeEmpty();
    }
}
