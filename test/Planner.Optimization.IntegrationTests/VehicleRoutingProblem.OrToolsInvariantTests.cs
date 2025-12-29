using Planner.Optimization;
using Planner.Optimization.IntegrationTests.Assertions;
using Planner.Testing.Fixtures;
using System;
using Xunit;

namespace Planner.Optimization.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class VehicleRoutingProblemOrToolsInvariantTests {
    [Fact]
    public void Real_solver_respects_all_model_invariants() {
        if (Environment.GetEnvironmentVariable("PLANNER_TEST_ORTOOLS") != "1")
            return;

        var solver = new VehicleRoutingProblem();
        var request = VrpBaseline.CreateSmallDeterministic();

        var response = solver.Optimize(request);

        // --- Core invariants ---
        response.ShouldNotAssignJobsMoreThanOnce();
        response.ShouldRespectCapacities(request);
        response.ShouldRespectTimeWindows(request);
        response.ShouldRespectVehicleUsageConsistency();
        response.ShouldHaveSaneCosts();
    }
}
