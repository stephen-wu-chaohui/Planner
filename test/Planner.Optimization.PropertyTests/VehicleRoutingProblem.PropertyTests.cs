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

}
