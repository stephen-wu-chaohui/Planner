using Microsoft.AspNetCore.Authorization;
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
[Authorize(Policy = "AdminOnly")]
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

        var vehicles = await db.Vehicles
            .Include(v => v.StartDepot)
                .ThenInclude(d => d.Location)
            .Include(v => v.EndDepot)
                .ThenInclude(d => d.Location)
            .ToListAsync();

        return new OptimizeRouteRequest(
            tenant.TenantId,
            OptimizationRunId: Guid.NewGuid(),
            RequestedAt: DateTime.UtcNow,
            Jobs: jobs.Select(ToJobInput).ToList(),
            Vehicles: vehicles.Select(ToVehicleInput).ToList(),
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

        if (vehicle.StartDepot?.Location is null)
            throw new InvalidOperationException($"Vehicle {vehicle.Id} missing StartDepot/Location");
        if (vehicle.EndDepot?.Location is null)
            throw new InvalidOperationException($"Vehicle {vehicle.Id} missing EndDepot/Location");

        return new VehicleInput(
            VehicleId: vehicle.Id,
            Name: vehicle.Name,
            ShiftLimitMinutes: vehicle.ShiftLimitMinutes,
            StartLocation: new LocationInput(
                LocationId: vehicle.DepotStartId,
                Address: vehicle.StartDepot.Location.Address,
                Latitude: vehicle.StartDepot.Location.Latitude,
                Longitude: vehicle.StartDepot.Location.Longitude
            ),
            EndLocation: new LocationInput(
                LocationId: vehicle.DepotEndId,
                Address: vehicle.EndDepot.Location.Address,
                Latitude: vehicle.EndDepot.Location.Latitude,
                Longitude: vehicle.EndDepot.Location.Longitude
            ),
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

