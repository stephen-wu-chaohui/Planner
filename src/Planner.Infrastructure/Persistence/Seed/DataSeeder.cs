using Microsoft.EntityFrameworkCore;
using Planner.Domain;
using Planner.Domain.Entities;
using Planner.Infrastructure.Persistence;

namespace Planner.Infrastructure.Seed;

public static class DataSeeder {
    public static async Task ResetAndSeedAsync(PlannerDbContext db) {
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();

        await SeedAsync(db);
    }

    public static async Task SeedAsync(PlannerDbContext context) {

        var cities = new[]
        {
            new { Name = "Taipei", Lat = 25.0330,  Lon = 121.5654 },
            new { Name = "Perth", Lat = -31.9523, Lon = 115.8613 },
            new { Name = "Sydney", Lat = -33.8688, Lon = 151.2093 },
            new { Name = "Melbourne", Lat = -37.8136, Lon = 144.9631 },
            new { Name = "Auckland", Lat = -36.8485, Lon = 174.7633 },
            new { Name = "Christchurch", Lat = -43.5321, Lon = 172.6362 }
        };

        var popularNames = new[] { "Oliver", "Noah", "Leo", "Henry", "Jack", "Charlotte", "Isla", "Amelia", "Mia", "Ava" };
        var random = new Random();

        foreach (var city in cities) {
            // 1. Generate Tenant and Depot
            var tenant = new Tenant {
                Id = Guid.NewGuid(),
                Name = city.Name
            };
            context.Tenants.Add(tenant);

            var depot = new Depot {
                TenantId = tenant.Id,
                Name = $"{city.Name} Main Depot",
                Location = new Location { Latitude = city.Lat, Longitude = city.Lon }
            };
            context.Depots.Add(depot);

            // 2. Generate 20 Customers for each tenant (within 10km)
            for (int i = 1; i <= 20; i++) {
                var offset = GetRandomOffset(random, 10);
                var customer = new Customer {
                    TenantId = tenant.Id,
                    Name = $"{city.Name} Client {i}",
                    Location = new Location {
                        Latitude = city.Lat + offset.Lat,
                        Longitude = city.Lon + offset.Lon
                    },
                    DefaultServiceMinutes = 15
                };
                context.Customers.Add(customer);
                context.SaveChanges(); // Save to get CustomerId for Job creation

                // 4. Generate a Job for each customer
                var job = new Job {
                    TenantId = tenant.Id,
                    Name = $"Delivery to {customer.Name}",
                    CustomerID = customer.CustomerId,
                    JobType = JobType.Delivery,
                    Location = customer.Location,
                    ServiceTimeMinutes = 15,
                    Reference = $"REF-{tenant.Name.ToUpper()}-{i:D3}"
                };
                context.Jobs.Add(job);
            }

            // 3. Generate 4 Drivers (Vehicles) for each tenant
            for (int j = 0; j < 4; j++) {
                var vehicle = new Vehicle {
                    TenantId = tenant.Id,
                    Name = popularNames[random.Next(popularNames.Length)],
                    DepotStartId = depot.Id,
                    DepotEndId = depot.Id,
                    MaxPallets = 10,
                    FuelRatePerKm = 1.5
                };
                context.Vehicles.Add(vehicle);
            }
        }

        context.SaveChanges();
    }

    private static (double Lat, double Lon) GetRandomOffset(Random rand, double radiusKm) {
        // Simple approximation: 1 degree lat is ~111km
        double latOffset = (rand.NextDouble() * 2 - 1) * (radiusKm / 111.0);
        // Longitude degree distance varies by latitude; using 111km as a rough base for these regions
        double lonOffset = (rand.NextDouble() * 2 - 1) * (radiusKm / 111.0);
        return (latOffset, lonOffset);
    }
}