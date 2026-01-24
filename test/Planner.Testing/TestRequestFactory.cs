using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Planner.API.Services;
using Planner.Domain;
using System.Linq;
using System.Security.Claims;

namespace Planner.Testing;

public static class TestRequestFactory {

    public static OptimizeRouteRequest CreateSimpleRequest(
        int vehicleCount = 2,
        int jobCount = 4
    ) {
        var tenantId = Guid.NewGuid();
        var runId = Guid.NewGuid();

        // ---- Depots --------------------------------------------------
        // Depots are derived from vehicle StartLocation/EndLocation.
        var depotLocation = new Location {
            Id = 1,
            Address = "Perth",
            Latitude = -31.9505,
            Longitude = 115.8605
        };

        // ---- Vehicles ------------------------------------------------
        var vehicles = Enumerable.Range(1, vehicleCount)
            .Select(i => new Vehicle {
                Id = i,
                TenantId = tenantId,
                Name = $"Vehicle {i}",
                DepotStartId = depotLocation.Id,
                DepotEndId = depotLocation.Id,
                StartDepot = new Depot { Location = depotLocation },
                EndDepot = new Depot { Location = depotLocation },
                MaxPallets = 10,
                MaxWeight = 1_000,
                RefrigeratedCapacity = 5,
                SpeedFactor = 1.0,
                ShiftLimitMinutes = 8 * 60,
                BaseFee = 10.0
            })
            .ToList();

        // ---- Jobs ----------------------------------------------------
        var jobs = Enumerable.Range(1, jobCount)
            .Select(i => new Job { 
                Id = i,
                TenantId = tenantId,
                Name = $"Job {i}",
                JobType = JobType.Delivery,
                Location = new Location { 
                    Id = 100 + i,
                    Address = $"Customer {i} Address",
                    Latitude = -31.9505 + (i * 0.01),
                    Longitude = 115.8605 + (i * 0.01)
                },
                ReadyTime = 0,
                DueTime = 24 * 60,
                ServiceTimeMinutes = 10,
                PalletDemand = 1,
                WeightDemand = 100,
                RequiresRefrigeration = false
            })
            .ToList();

        // ---- Build Matrices -----------------------------------------
        var settings = FastSettings();
        var allLocations = new[] { depotLocation }
            .Concat(jobs.Select(j => j.Location))
            .ToList();
        
        var (distanceMatrix, travelTimeMatrix) = MatrixBuilder.BuildMatrices(allLocations, settings);

        // ---- Request -------------------------------------------------
        return new OptimizeRouteRequest(
            TenantId: tenantId,
            OptimizationRunId: runId,
            RequestedAt: DateTime.UtcNow,
            Vehicles: vehicles.Select(ToInput.ToVehicleInput).ToList(),
            Jobs: jobs.Select(ToInput.ToJobInput).ToList(),
            DistanceMatrix: distanceMatrix,
            TravelTimeMatrix: travelTimeMatrix,
            Settings: settings,
            OvertimeMultiplier: 2.0
        );
    }

    public static ClaimsPrincipal CreateAdminUser() {
        var claims = new[] {
            new Claim(ClaimTypes.Name, "TestAdmin"),
            new Claim(ClaimTypes.Role, "Admin"), // Assuming AdminOnly policy looks for this
            new Claim("scope", "admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        return new ClaimsPrincipal(identity);
    }

    public static void MockUserContext(this ControllerBase controller) {
        controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext { User = CreateAdminUser() }
        };
    }

    public static OptimizationSettings FastSettings() =>
        new() {
            SearchTimeLimitSeconds = 5
        };
}
