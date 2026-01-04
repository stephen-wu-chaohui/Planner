using Planner.Contracts.Optimization.Requests;
using Planner.Testing;
using Planner.Testing.Builders;
using System;
using System.Linq;

namespace Planner.Optimization.IntegrationTests.Generators;

public static class RandomVrpRequestFactory {
    public static OptimizeRouteRequest Create(
        int seed,
        int depotCount,
        int jobCount,
        int vehicleCount) {
        var rand = new Random(seed);

        // --- Depots (derived from vehicles) ---
        var depotLocations = Enumerable.Range(1, depotCount)
            .Select(i => {
                var locId = 1000 + i;
                return LocationInputBuilder.Create()
                    .WithId(locId)
                    .WithLatLng(
                        -31.95 + rand.NextDouble() * 0.05,
                        115.86 + rand.NextDouble() * 0.05)
                    .Build();
            })
            .ToList();

        // --- Jobs ---
        var jobs = Enumerable.Range(1, jobCount)
            .Select(i => {
                var jobId = 10 + i;
                var locId = 2000 + i;

                return JobInputBuilder.Create()
                    .WithJobId(jobId)
                    .WithLocation(
                        LocationInputBuilder.Create()
                            .WithId(locId)
                            .WithLatLng(
                                -31.95 + rand.NextDouble() * 0.05,
                                115.86 + rand.NextDouble() * 0.05)
                            .Build())
                    .WithService(rand.Next(5, 20))
                    .WithTimeWindow(0, 720)
                    .WithDemand(
                        pallets: rand.Next(0, 3),
                        weight: rand.Next(0, 50))
                    .Build();
            })
            .ToList();

        // --- Vehicles ---
        var vehicles = Enumerable.Range(1, vehicleCount)
            .Select(i => {
                var depot = depotLocations[rand.Next(depotLocations.Count)];

                return VehicleInputBuilder.Create()
                    .WithVehicleId(i)
                    .WithDepot(depot.LocationId, depot.LocationId)
                    .WithCapacity(
                        pallets: 10,
                        weight: 500)
                    .WithCosts(
                        perMin: 1.0,
                        perKm: 1.0)
                    .Build();
            })
            .ToList();

        return OptimizeRouteRequestBuilder.Create()
            .WithTenant(TestIds.TenantId)
            .WithRunId(Guid.NewGuid())
            .WithJobs(jobs)
            .WithVehicles(vehicles)
            .Build();
    }
}
