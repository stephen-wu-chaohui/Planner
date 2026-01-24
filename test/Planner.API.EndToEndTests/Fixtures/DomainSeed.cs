using Planner.Domain;
using Planner.Infrastructure.Persistence;
using System;

namespace Planner.API.EndToEndTests.Fixtures;

public static class DomainSeed {
    public static void SeedSmallScenario(PlannerDbContext db, Guid tenantId) {
        // 1. Create a Location for the Depot
        var depotLocation = new Location {
            Id = 1,
            // TenantId = tenantId,
            Address = "Central Depot",
            Latitude = -31.95,
            Longitude = 115.86
        };

        // 2. Create the Depot (Required by Vehicle mapping in Controller)
        var depot = new Depot {
            Id = 1,
            TenantId = tenantId,
            Location = depotLocation,
            Name = "Main Hub"
        };

        // 3. Create a Vehicle linked to the Depot
        var vehicle = new Vehicle {
            Id = 1,
            TenantId = tenantId,
            Name = "Truck 01",
            StartDepot = depot, // Matches .Include(v => v.StartDepot)
            EndDepot = depot,   // Matches .Include(v => v.EndDepot)
            MaxPallets = 10,
            MaxWeight = 1000,
            ShiftLimitMinutes = 480,
            DriverRatePerHour = 50,
            MaintenanceRatePerHour = 10,
            FuelRatePerKm = 1.5,
            BaseFee = 20,
            SpeedFactor = 1.0
        };

        // 4. Create a Job with a Location
        var jobLocation = new Location {
            Id = 2,
            // TenantId = tenantId,
            Address = "Customer A",
            Latitude = -31.96,
            Longitude = 115.87
        };

        var job = new Job {
            Id = 1,
            TenantId = tenantId,
            Name = "Delivery 1",
            Location = jobLocation, // Matches .Include(j => j.Location)
            JobType = JobType.Delivery,
            ServiceTimeMinutes = 15,
            ReadyTime = 0,
            DueTime = 1440,
            PalletDemand = 2,
            WeightDemand = 200
        };

        var customer = new Customer {
            CustomerId = 1,
            TenantId = tenantId,
            Name = "Customer A",
            Location = jobLocation, // Matches .Include(j => j.Location)
            DefaultServiceMinutes = 15,
            RequiresRefrigeration = false,
        };

        db.Locations.AddRange(depotLocation, jobLocation);
        db.Depots.Add(depot);
        db.Vehicles.Add(vehicle);
        db.Jobs.Add(job);
        db.Customers.Add(customer);

        db.SaveChanges(); // Important: Persist before controller query
    }
}