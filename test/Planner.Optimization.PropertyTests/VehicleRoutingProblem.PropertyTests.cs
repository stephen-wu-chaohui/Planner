using FluentAssertions;
using Planner.Testing;
using System;
using Xunit;

namespace Planner.Optimization.PropertyTests;

public sealed class VehicleRoutingProblemPropertyTests {

    // ---------------------------------------
    // VALID INPUTS NEVER FAIL VALIDATION
    // ---------------------------------------
    [Fact]
    public void Valid_requests_do_not_throw_validation_exceptions() {
        // Arrange
        var request = TestRequestFactory.CreateSimpleRequest();

        var solver = new VehicleRoutingProblem();

        Action act = () => solver.Optimize(request);

        act.Should().NotThrow<SolverInputInvalidException>();
    }

    // ---------------------------------------
    // DUPLICATE DEPOT IDS ALWAYS FAIL
    // ---------------------------------------
    [Fact]
    public void Duplicate_depot_location_ids_should_throw() {
        // Arrange
        var request = TestRequestFactory.CreateSimpleRequest();

        // Force duplicate depot LocationId
        var duplicateDepot = request.Depots[0];

        request = request with {
            Depots = new[] {
                duplicateDepot,
                duplicateDepot
            }
        };

        var solver = new VehicleRoutingProblem();

        // Act
        Action act = () => solver.Optimize(request);

        // Assert
        act.Should().Throw<SolverInputInvalidException>()
            .WithMessage("*Duplicate Depot*");
    }
}
