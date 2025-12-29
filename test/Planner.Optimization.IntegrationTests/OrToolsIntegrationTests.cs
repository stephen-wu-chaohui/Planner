using FluentAssertions;
using Planner.Optimization;
using Planner.Testing.Assertions;
using Planner.Testing.Fixtures;
using System;
using Xunit;

namespace Planner.Optimization.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class OrToolsIntegrationTests {

    [Fact]
    public void Real_solver_produces_valid_routes_when_enabled() {
        if (Environment.GetEnvironmentVariable("PLANNER_TEST_ORTOOLS") != "1")
            return;

        var solver = new VehicleRoutingProblem();
        var request = VrpBaseline.CreateSmallDeterministic();

        var response = solver.Optimize(request);

        response.ShouldBeValidBasicShape(request);
        response.ShouldHaveNoDuplicateAssignedJobs();
        response.Routes.Should().NotBeEmpty();
    }
}
