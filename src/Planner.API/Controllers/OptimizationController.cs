using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.Application;
using Planner.Contracts.Optimization.Inputs;
using Planner.Contracts.Optimization.Requests;
using Planner.Domain;
using Planner.Infrastructure.Persistence;
using Planner.Messaging;

namespace Planner.API.Controllers;

[ApiController]
[Route("api/vrp")]
public class OptimizationController(
    IMessageBus bus,
    PlannerDbContext db,
    ITenantContext tenant) : ControllerBase {
    /// <summary>
    /// Accept a route optimization request and dispatch it to the optimization worker.
    /// </summary>
    [HttpGet("solve")]
    public async Task<IActionResult> Solve() {
        var request = await BuildRequestFromDomainAsync();

        if (request.Jobs.Count == 0 || request.Vehicles.Count == 0)
            return BadRequest("No jobs or vehicles available for optimization.");

        await bus.PublishAsync(MessageRoutes.Request, request);

        return Ok(request.Settings);
    }

    private async Task<OptimizeRouteRequest> BuildRequestFromDomainAsync() {
        var jobs = await db.Jobs
            .Include(j => j.Location)
            .ToListAsync();

        var vehicles = await db.Vehicles.ToListAsync();

        var depots = await db.Depots
            .Include(d => d.Location)
            .ToListAsync();

        return new OptimizeRouteRequest(
            tenant.TenantId,
            OptimizationRunId: Guid.NewGuid(),
            RequestedAt: DateTime.UtcNow,
            Jobs: jobs.Select(ToJobInput).ToList(),
            Vehicles: vehicles.Select(ToVehicleInput).ToList(),
            Depots: depots.Select(d => new DepotInput(ToLocationInput(d.Location))).ToList(),
            Settings: new OptimizationSettings(
                SearchTimeLimitSeconds: 6 * jobs.Count // 6 seconds per job
            )
        );
    }

    private static JobInput ToJobInput(Job job) {
        return new JobInput(
            JobId: job.Id,
            JobType: ToContractJobType(job.JobType),
            Name: job.Name,
            Location: ToLocationInput(job.Location),
            ServiceTimeMinutes: job.ServiceTimeMinutes,
            ReadyTime: job.ReadyTime,
            DueTime: job.DueTime,
            PalletDemand: job.PalletDemand,
            WeightDemand: job.WeightDemand,
            RequiresRefrigeration: job.RequiresRefrigeration
        );
    }

    private static VehicleInput ToVehicleInput(Vehicle vehicle) {
        var costPerMinute = (vehicle.DriverRatePerHour + vehicle.MaintenanceRatePerHour) / 60.0;

        return new VehicleInput(
            VehicleId: vehicle.Id,
            Name: vehicle.Name,
            ShiftLimitMinutes: vehicle.ShiftLimitMinutes,
            DepotStartId: vehicle.DepotStartId,
            DepotEndId: vehicle.DepotEndId,
            SpeedFactor: vehicle.SpeedFactor,
            CostPerMinute: costPerMinute,
            CostPerKm: vehicle.FuelRatePerKm,
            BaseFee: vehicle.BaseFee,
            MaxPallets: vehicle.MaxPallets,
            MaxWeight: vehicle.MaxWeight,
            RefrigeratedCapacity: vehicle.RefrigeratedCapacity
        );
    }

    private static LocationInput ToLocationInput(Location location) {
        return new LocationInput(
            LocationId: location.Id,
            Address: location.Address,
            Latitude: location.Latitude,
            Longitude: location.Longitude
        );
    }

    private static int ToContractJobType(JobType jobType) {
        // Domain enum is: 0 Depot, 1 Pickup, 2 Delivery
        // Contract expects: 0 Depot, 1 Delivery, 2 Pickup
        return jobType switch {
            JobType.Depot => 0,
            JobType.Delivery => 1,
            JobType.Pickup => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(jobType), jobType, "Unknown job type.")
        };
    }
}

