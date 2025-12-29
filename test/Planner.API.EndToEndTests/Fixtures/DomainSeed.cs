using Planner.Domain;
using Planner.Domain.Entities;
using Planner.Infrastructure.Persistence;
using System;

namespace Planner.API.EndToEndTests.Fixtures;

public static class DomainSeed {
    public static void SeedSmallScenario(
        PlannerDbContext db,
        Guid tenantId) {
        var depotLocation = new Location(
            id: 1001,
            address: "Depot",
            latitude: -31.95,
            longitude: 115.86);

        var depot = new Depot {
            TenantId = tenantId,
            Name = "Main Depot",
            Location = depotLocation
        };

        var vehicle = new Vehicle {
            TenantId = tenantId,
            Name = "Van-1",
            DepotStartId = 1001,
            DepotEndId = 1001,
            MaxPallets = 10,
            MaxWeight = 1000,
            RefrigeratedCapacity = 0,
            DriverRatePerHour = 60,
            MaintenanceRatePerHour = 0,
            FuelRatePerKm = 1,
            BaseFee = 0
        };

        var jobLocation = new Location(
            id: 2001,
            address: "Customer",
            latitude: -31.94,
            longitude: 115.87);

        var job = new Job {
            TenantId = tenantId,
            Name = "Delivery",
            JobType = JobType.Delivery,
            Location = jobLocation,
            ReadyTime = 0,
            DueTime = 720,
            ServiceTimeMinutes = 10,
            PalletDemand = 1,
            WeightDemand = 10
        };

        db.Depots.Add(depot);
        db.Vehicles.Add(vehicle);
        db.Jobs.Add(job);

        db.SaveChanges();
    }
}
