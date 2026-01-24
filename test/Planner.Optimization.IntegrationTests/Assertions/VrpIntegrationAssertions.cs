using FluentAssertions;
using Planner.Messaging.Optimization;
using Planner.Messaging.Optimization.Responses;
using System;
using System.Linq;

namespace Planner.Optimization.IntegrationTests.Assertions;

public static class VrpIntegrationAssertions {
    public static void ShouldRespectCapacities(
        this OptimizeRouteResponse resp,
        OptimizeRouteRequest req) {
        var vehicles = req.Vehicles.ToDictionary(v => v.VehicleId);

        foreach (var route in resp.Routes.Where(r => r.Used)) {
            var vehicle = vehicles[route.VehicleId];

            long maxPallets = 0;
            long maxWeight = 0;
            long maxRefrig = 0;

            foreach (var stop in route.Stops) {
                maxPallets = Math.Max(maxPallets, stop.PalletLoad);
                maxWeight = Math.Max(maxWeight, stop.WeightLoad);
                maxRefrig = Math.Max(maxRefrig, stop.RefrigeratedLoad);
            }

            maxPallets.Should().BeLessThanOrEqualTo(vehicle.MaxPallets);
            maxWeight.Should().BeLessThanOrEqualTo(vehicle.MaxWeight);
            maxRefrig.Should().BeLessThanOrEqualTo(vehicle.RefrigeratedCapacity);
        }
    }

    public static void ShouldRespectTimeWindows(
        this OptimizeRouteResponse resp,
        OptimizeRouteRequest req) {
        var jobs = req.Jobs.ToDictionary(j => j.JobId);

        foreach (var route in resp.Routes.Where(r => r.Used)) {
            foreach (var stop in route.Stops) {
                var job = jobs[stop.JobId];

                stop.ArrivalTime.Should().BeGreaterThanOrEqualTo(job.ReadyTime);
                stop.ArrivalTime.Should().BeLessThanOrEqualTo(job.DueTime);
                stop.DepartureTime.Should()
                    .BeGreaterThanOrEqualTo(stop.DepartureTime);
            }
        }
    }

    public static void ShouldNotAssignJobsMoreThanOnce(
        this OptimizeRouteResponse resp) {
        var jobIds = resp.Routes
            .Where(r => r.Used)
            .SelectMany(r => r.Stops)
            .Select(s => s.JobId)
            .ToList();

        jobIds.Should().OnlyHaveUniqueItems();
    }

    public static void ShouldRespectVehicleUsageConsistency(
        this OptimizeRouteResponse resp) {
        foreach (var route in resp.Routes) {
            if (!route.Used) {
                route.Stops.Should().BeEmpty();
                route.TotalMinutes.Should().Be(0);
                route.TotalDistanceKm.Should().Be(0);
                route.TotalCost.Should().Be(0);
            } else {
                route.Stops.Should().NotBeEmpty();
                route.TotalMinutes.Should().BeGreaterThan(0);
            }
        }
    }

    public static void ShouldHaveSaneCosts(this OptimizeRouteResponse resp) {
        resp.TotalCost.Should().BeGreaterThanOrEqualTo(0);

        foreach (var route in resp.Routes) {
            route.TotalCost.Should().BeGreaterThanOrEqualTo(0);
        }
    }
}
