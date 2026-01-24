using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        var depotLocation = new LocationInput(
            LocationId: 1,
            Latitude: -31.95,
            Longitude: 115.86,
            Address: "Depot"
        );

        // ---- Vehicles ------------------------------------------------
        var vehicles = Enumerable.Range(1, vehicleCount)
            .Select(i => new VehicleInput(
                VehicleId: i,
                Name: $"Vehicle {i}",
                StartLocation: depotLocation,
                EndLocation: depotLocation,
                MaxPallets: 10,
                MaxWeight: 1_000,
                RefrigeratedCapacity: 5,
                SpeedFactor: 1.0,
                ShiftLimitMinutes: 8 * 60,
                CostPerMinute: 1.0,
                CostPerKm: 1.0,
                BaseFee: 10.0
            ))
            .ToList();

        // ---- Jobs ----------------------------------------------------
        var jobs = Enumerable.Range(1, jobCount)
            .Select(i => new JobInput(
                JobId: i,
                JobType: 1, // JobType.Delivery,
                Name: $"Job {i}",
                Location: new LocationInput(
                    LocationId: 100 + i,
                    Latitude: -31.95 + i * 0.01,
                    Longitude: 115.86 + i * 0.01,
                    Address: $"Job {i}"
                ),
                ReadyTime: 0,
                DueTime: 24 * 60,
                ServiceTimeMinutes: 10,
                PalletDemand: 1,
                WeightDemand: 100,
                RequiresRefrigeration: false
            ))
            .ToList();

        // ---- Request -------------------------------------------------
        return new OptimizeRouteRequest(
            TenantId: tenantId,
            OptimizationRunId: runId,
            RequestedAt: DateTime.UtcNow,
            Vehicles: vehicles,
            Jobs: jobs,
            Settings: FastSettings(),
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
