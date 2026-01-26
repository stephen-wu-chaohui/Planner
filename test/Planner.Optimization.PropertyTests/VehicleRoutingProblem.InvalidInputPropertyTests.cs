using FluentAssertions;
using Planner.Messaging.Optimization.Inputs;
using Planner.Testing;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Planner.Optimization.PropertyTests;

public sealed class VehicleRoutingProblemInvalidInputTests {

    private readonly VehicleRoutingProblem _sut = new();

    // All tests use fast settings
    private static OptimizeRouteRequest WithFastSettings(OptimizeRouteRequest req) =>
        req with {
            Settings = new OptimizationSettings {
                SearchTimeLimitSeconds = 1
            }
        };

    [Fact]
    public void Job_and_depot_location_id_should_list() {
        // Arrange
        var request = TestRequestFactory.CreateSimpleRequest();

        var job = request.Stops[0];

        var collidingJob = job;

        request = WithFastSettings(request with {
            Stops = request.Stops
                .Skip(1)
                .Append(collidingJob)
                .ToArray()
        });

        // Act
        var response = _sut.Optimize(request);

        // Assert
        response.ErrorMessage.Should().BeNullOrEmpty();
    }

    [Fact]
    public void Vehicle_with_missing_start_depot_should_return_error() {
        // Arrange
        var request = TestRequestFactory.CreateSimpleRequest();

        var vehicle = request.Vehicles[0];

        var invalidVehicle = vehicle with {
            StartDepotLocationId = 999999 // not in depots
        };

        request = WithFastSettings(request with {
            Vehicles = request.Vehicles
                .Skip(1)
                .Append(invalidVehicle)
                .ToArray()
        });

        // Act
        var response = _sut.Optimize(request);

        // Assert
        response.ErrorMessage.Should().NotBeNullOrEmpty();
        response.ErrorMessage.Should().Contain("references missing DepotId.");
    }

    [Fact]
    public void Vehicle_with_missing_end_depot_should_return_error() {
        // Arrange
        var request = TestRequestFactory.CreateSimpleRequest();

        var vehicle = request.Vehicles[0];

        var invalidLocationId = 100;

        var invalidVehicle = vehicle with {
            EndDepotLocationId = invalidLocationId // not in depots
        };

        request = WithFastSettings(request with {
            Vehicles = request.Vehicles
                .Skip(1)
                .Append(invalidVehicle)
                .ToArray()
        });

        // Act
        var response = _sut.Optimize(request);

        // Assert
        response.ErrorMessage.Should().NotBeNullOrEmpty();
        response.ErrorMessage.Should().Contain("references missing DepotId.");
    }

}
