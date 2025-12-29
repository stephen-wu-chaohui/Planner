using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Planner.API.Controllers;
using Planner.API.EndToEndTests.Fixtures;
using Planner.Application;
using Planner.Contracts.Optimization.Requests;
using Planner.Infrastructure.Persistence;
using Planner.Messaging;
using System.Threading.Tasks;
using Xunit;

namespace Planner.API.EndToEndTests.Tests;

public sealed class OptimizationControllerEndToEndTests {
    [Fact]
    public async Task Solve_endpoint_executes_full_pipeline_successfully() {
        using var factory = new TestApiFactory();

        var tenant = factory.Get<ITenantContext>();
        var db = factory.Get<PlannerDbContext>();
        var controller = factory.Get<OptimizationController>();

        DomainSeed.SeedSmallScenario(db, tenant.TenantId);

        var result = await controller.Solve();

        result.Should().NotBeNull();

        // Build request directly to validate solver invariants
        var method = controller
            .GetType()
            .GetMethod("BuildRequestFromDomainAsync",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

        method.Should().NotBeNull();

        var request = await (Task<OptimizeRouteRequest>) method!.Invoke(controller, null)!;

        request.Should().NotBeNull();

        var bus = factory.Get<IMessageBus>() as FakeMessageBus;

        bus.Should().NotBeNull();
        bus!.PublishedMessages.Should().ContainSingle(m =>
            m.Route == MessageRoutes.Request);
    }
}
