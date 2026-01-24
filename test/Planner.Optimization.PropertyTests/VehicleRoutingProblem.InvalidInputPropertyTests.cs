using FluentAssertions;
using Planner.Messaging.Optimization;
using Planner.Messaging.Optimization.Requests;
using Planner.Testing;
using System;
using System.Linq;
using Xunit;

namespace Planner.Optimization.InvalidInputTests;

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
    public void Job_and_depot_location_id_collision_should_throw() {
        // Arrange
        var request = TestRequestFactory.CreateSimpleRequest();

        var depotLocId = request.Vehicles[0].StartLocation.LocationId;
        var job = request.Jobs[0];

        var collidingJob = job with {
            Location = job.Location with {
                LocationId = depotLocId
            }
        };

        request = WithFastSettings(request with {
            Jobs = request.Jobs
                .Skip(1)
                .Append(collidingJob)
                .ToList()
        });

        // Act
        Action act = () => _sut.Optimize(request);

        // Assert
        act.Should()
           .Throw<SolverInputInvalidException>()
           .WithMessage("*Job/Depot LocationId collision*");
    }

    [Fact]
    public void Vehicle_with_missing_start_depot_should_throw() {
        // Arrange
        var request = TestRequestFactory.CreateSimpleRequest();

        var vehicle = request.Vehicles[0];

        var invalidVehicle = vehicle with {
            StartLocation = vehicle.StartLocation with { LocationId = 999999 } // not in depots
        };

        request = WithFastSettings(request with {
            Vehicles = request.Vehicles
                .Skip(1)
                .Append(invalidVehicle)
                .ToList()
        });

        // Act
        Action act = () => _sut.Optimize(request);

        // Assert
        act.Should()
           .Throw<SolverInputInvalidException>()
           .WithMessage("*references missing DepotId*");
    }

    [Fact]
    public void Vehicle_with_missing_end_depot_should_throw() {
        // Arrange
        var request = TestRequestFactory.CreateSimpleRequest();

        var vehicle = request.Vehicles[0];

        var invalidVehicle = vehicle with {
            EndLocation = vehicle.EndLocation with { LocationId = 888888 } // not in depots
        };

        request = WithFastSettings(request with {
            Vehicles = request.Vehicles
                .Skip(1)
                .Append(invalidVehicle)
                .ToList()
        });

        // Act
        Action act = () => _sut.Optimize(request);

        // Assert
        act.Should()
           .Throw<SolverInputInvalidException>()
           .WithMessage("*references missing DepotId*");
    }

}
