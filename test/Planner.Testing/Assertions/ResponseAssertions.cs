using Planner.Messaging.Optimization.Inputs;
using Planner.Messaging.Optimization.Outputs;

namespace Planner.Testing.Assertions;

public static class ResponseAssertions {
    public static void ShouldBeValidBasicShape(this OptimizeRouteResponse resp, OptimizeRouteRequest req) {
        resp.TenantId.Should().Be(req.TenantId);
        resp.OptimizationRunId.Should().Be(req.OptimizationRunId);

        // Either solver succeeded or returned an “empty” result; but never null exceptions here.
        resp.Routes.Should().NotBeNull();
    }

    public static void ShouldHaveNoDuplicateAssignedJobs(this OptimizeRouteResponse resp) {
        var assignedJobIds = resp.Routes
            .Where(r => r.Stops.Any())
            .SelectMany(r => r.Stops)
            .Select(s => s.LocationId)
            .ToList();

        assignedJobIds.Should().OnlyHaveUniqueItems();
    }
}
