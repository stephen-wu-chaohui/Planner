using FluentAssertions;
using Planner.Testing;
using Xunit;

namespace Planner.Optimization.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class SmokingTests {
    [Fact]
    public void Optimize_Should_Return_Valid_Response_For_Simple_Request() {
        // Arrange
        var request = TestRequestFactory.CreateSimpleRequest();
        var sut = new VehicleRoutingProblem();

        // Act
        var response = sut.Optimize(request);

        // Assert
        response.TenantId.Should().Be(request.TenantId);
        response.OptimizationRunId.Should().Be(request.OptimizationRunId);
        response.Routes.Should().HaveCount(request.Vehicles.Count);

        foreach (var route in response.Routes) {
            route.TotalMinutes.Should().BeGreaterThanOrEqualTo(0);
            route.TotalDistanceKm.Should().BeGreaterThanOrEqualTo(0);
            route.TotalCost.Should().BeGreaterThanOrEqualTo(0);

            foreach (var task in route.Stops) {
                request.Jobs.Should().Contain(j => j.JobId == task.JobId);
            }
        }
    }
}
