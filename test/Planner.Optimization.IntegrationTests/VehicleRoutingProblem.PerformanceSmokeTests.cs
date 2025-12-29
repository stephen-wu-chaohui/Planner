using FluentAssertions;
using Planner.Testing.Fixtures;
using System;
using System.Diagnostics;
using Xunit;

namespace Planner.Optimization.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class VehicleRoutingProblemPerformanceSmokeTests {
    [Fact]
    public void Baseline_problem_solves_within_time_budget() {
        // Feature flag: skip unless explicitly enabled
        if (Environment.GetEnvironmentVariable("PLANNER_TEST_ORTOOLS") != "1")
            return;

        var solver = new VehicleRoutingProblem();
        var request = VrpBaseline.CreateSmallDeterministic();

        var sw = Stopwatch.StartNew();

        var response = solver.Optimize(request);

        sw.Stop();

        // --- Assertions ---
        response.Routes.Should().NotBeEmpty();

        // Conservative budget: seconds, not milliseconds
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2),
            $"Solver took {sw.Elapsed.TotalMilliseconds:N0} ms, which indicates a regression");
    }
}
