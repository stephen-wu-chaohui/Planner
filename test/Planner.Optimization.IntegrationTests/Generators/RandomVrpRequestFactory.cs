using Planner.Domain;
using Planner.Messaging.Optimization.Inputs;
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
                return new Depot {
                    Id = 9000 + i,
                    TenantId = TestIds.TenantId,
                    Name = $"Depot {i}",
                    LocationId = locId,
                    Location = new Location {
                        Id = locId,
                        Latitude = -31.95 + rand.NextDouble() * 0.05,
                        Longitude = 115.86 + rand.NextDouble() * 0.05
                    }
                };
            })
            .ToList();

        // --- Jobs ---
        var jobs = Enumerable.Range(1, jobCount)
            .Select(i => {
                var jobId = 10 + i;
                var locId = 2000 + i;

                return new Job {
                    Id = 9000 + i,
                    TenantId = TestIds.TenantId,
                    Name = $"Depot {i}",
                    Location = new Location {
                        Id = locId,
                        Latitude = -31.95 + rand.NextDouble() * 0.05,
                        Longitude = 115.86 + rand.NextDouble() * 0.05
                    },
                    LocationId = locId,
                    ServiceTimeMinutes = 0,
                    PalletDemand = 0,
                    WeightDemand = 0
                };
            })
            .ToList();

        // --- Vehicles ---
        var vehicles = Enumerable.Range(1, vehicleCount)
            .Select(i => {
                var depot = depotLocations[rand.Next(depotLocations.Count)];

                return new Vehicle {
                    Id = i,
                    TenantId = TestIds.TenantId,
                    Name = $"Vehicle {i}",
                    SpeedFactor = 1.0,
                    ShiftLimitMinutes = 720,
                    DepotStartId = depot.LocationId,
                    DepotEndId = depot.LocationId,
                    DriverRatePerHour = 20.0,
                    MaintenanceRatePerHour = 10.0,
                    FuelRatePerKm = 0.5,
                    BaseFee = 50.0,
                    MaxPallets = 10,
                    MaxWeight = 500,
                    RefrigeratedCapacity = 0
                };
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
