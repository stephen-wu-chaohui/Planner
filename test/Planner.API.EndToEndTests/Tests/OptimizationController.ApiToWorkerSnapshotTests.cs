using FluentAssertions;
using Planner.API.Controllers;
using Planner.API.EndToEndTests.Fixtures;
using Planner.API.EndToEndTests.Snapshot;
using Planner.Application;
using Planner.Infrastructure.Persistence;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using VerifyXunit;
using Planner.Messaging.Optimization.Inputs;

namespace Planner.API.EndToEndTests.Tests;

public sealed class OptimizationControllerApiToWorkerSnapshotTests // Inherit from VerifyBase
{
    [Fact]
    public async Task OptimizeRouteRequest_published_by_API_is_stable() {
        using var factory = new TestApiFactory();

        var tenant = factory.Get<ITenantContext>();
        var db = factory.Get<PlannerDbContext>();
        var controller = factory.Get<OptimizationController>();

        // Seed domain
        DomainSeed.SeedSmallScenario(db, tenant.TenantId);

        // --- Correct async reflection ---
        var method = typeof(OptimizationController)
            .GetMethod(
                "BuildRequestFromDomainAsync",
                BindingFlags.NonPublic | BindingFlags.Instance);

        method.Should().NotBeNull();

        var task = (Task<OptimizeRouteRequest>)method!.Invoke(controller, null)!;

        var request = await task;

        // Snapshot the API → Worker message
        var snapshot = OptimizeRouteRequestSnapshot.Create(request);

        await Verifier.Verify(snapshot);
    }
}
