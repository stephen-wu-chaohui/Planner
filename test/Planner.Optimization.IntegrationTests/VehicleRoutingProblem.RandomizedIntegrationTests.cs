using Planner.Optimization;
using Planner.Optimization.IntegrationTests.Assertions;
using Planner.Optimization.IntegrationTests.Generators;
using System;
using Xunit;

namespace Planner.Optimization.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class VehicleRoutingProblemRandomizedIntegrationTests {
    [Fact]
    public void Solver_respects_invariants_across_randomized_scenarios() {
        if (Environment.GetEnvironmentVariable("PLANNER_TEST_ORTOOLS") != "1")
            return;

        var solver = new VehicleRoutingProblem();

        // Small, controlled exploration space
        const int iterations = 10;

        for (int i = 0; i < iterations; i++) {
            var request = RandomVrpRequestFactory.Create(
                seed: 1000 + i,
                depotCount: 1 + (i % 2),     // 1–2 depots
                jobCount: 3 + (i % 4),       // 3–6 jobs
                vehicleCount: 1 + (i % 2));  // 1–2 vehicles

            var response = solver.Optimize(request);

            // --- Invariants ---
            response.ShouldNotAssignJobsMoreThanOnce();
            response.ShouldRespectCapacities(request);
            response.ShouldRespectTimeWindows(request);
            response.ShouldRespectVehicleUsageConsistency();
            response.ShouldHaveSaneCosts();
        }
    }
}
