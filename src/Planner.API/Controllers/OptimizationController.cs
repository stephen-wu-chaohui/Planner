using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.API.Services;
using Planner.Application;
using Planner.Contracts.Optimization;
using Planner.Domain;
using Planner.Infrastructure.Persistence;
using Planner.Messaging;
using Planner.Messaging.Messaging;
using Planner.Messaging.Optimization.Inputs;

namespace Planner.API.Controllers;

[ApiController]
[Route("api/vrp")]
[Authorize(Policy = "AdminOnly")]
public class OptimizationController(
    IMessageBus bus,
    PlannerDbContext db,
    ITenantContext tenant,
    IMatrixCalculationService matrixService) : ControllerBase {
    /// <summary>
    /// Accept a route optimization request and dispatch it to the optimization worker.
    /// </summary>
    [HttpGet("solve")]
    public async Task<IActionResult> Solve() {
        var request = await BuildRequestFromDomainAsync();

        if (request.Stops.Length == 0 || request.Vehicles.Length == 0)
            return BadRequest("No jobs or vehicles available for optimization.");

        await bus.PublishAsync(MessageRoutes.Request, request);

        var summary = new OptimizationSummary(
            request.TenantId,
            request.OptimizationRunId,
            request.Stops.Length,
            request.Vehicles.Length,
            request.RequestedAt,
            request.Settings?.SearchTimeLimitSeconds ?? 60
        );
        return Ok(summary);
    }

    private async Task<OptimizeRouteRequest> BuildRequestFromDomainAsync() {

        EnsureJobsForAllCustomers();

        var jobs = await db.Jobs
            .Include(j => j.Location)
            .ToListAsync();

        var vehicles = await db.Vehicles
            .Include(v => v.StartDepot)
                .ThenInclude(d => d.Location)
            .Include(v => v.EndDepot)
                .ThenInclude(d => d.Location)
            .ToListAsync();

        var settings = new OptimizationSettings(
            SearchTimeLimitSeconds: 1 * jobs.Count // 1 second per job
        );

        // Build location list in the same order as solver expects: depots first, then jobs
        var depotLocations = vehicles
            .SelectMany(v => new[] { v.StartDepot.Location, v.EndDepot.Location })
            .GroupBy(l => l.Id)
            .Select(g => g.First())
            .ToList();

        var jobLocations = jobs.Select(j => j.Location).ToList();
        var allLocations = depotLocations.Concat(jobLocations).ToList();

        // Build matrices
        var (distanceMatrix, travelTimeMatrix) = matrixService.BuildMatrices(allLocations, settings);

        var stops = allLocations.Select(loc => {
            var job = jobs.FirstOrDefault(j => j.LocationId == loc.Id);
            if (job != null) {
                return ToInput.FromJob(job);
            }
            var depotLoc = depotLocations.FirstOrDefault(depot => depot.Id == loc.Id);
            if (depotLoc != null) {
                return ToInput.FromDepotLocation(depotLoc.Id);
            }
            return ToInput.FromDepotLocation(1); // Fallback, should not happen
        }).ToArray();

        var vs = vehicles.Select(ToInput.FromVehicle).ToArray();

        var request =  new OptimizeRouteRequest(
            TenantId: tenant.TenantId,
            OptimizationRunId: Guid.NewGuid(),
            RequestedAt: DateTime.UtcNow,
            Stops: stops,
            Vehicles: vs,
            DistanceMatrix: distanceMatrix,
            TravelTimeMatrix: travelTimeMatrix,
            Settings: settings
        );

        return request;
    }

    private void EnsureJobsForAllCustomers() {
        var jobsWithNoCustomers = db.Jobs
            .Where(j => !db.Customers.Any(c => c.CustomerId == j.CustomerId))
            .ToList();
        db.Jobs.RemoveRange(jobsWithNoCustomers);

        var customersWithNoJobs = db.Customers
            .Where(c => !db.Jobs.Any(j => j.CustomerId == c.CustomerId))
            .ToList();
        // 1. Create a list of new Job objects for each customer found
        var newJobs = customersWithNoJobs.Select(c => new Job {
            CustomerId = c.CustomerId,
            TenantId = tenant.TenantId,
            Name = $"Job for {c.Name}",
            LocationId = c.LocationId,
            Location = c.Location,
            JobType = JobType.Delivery,
            ServiceTimeMinutes = c.DefaultServiceMinutes,
            PalletDemand = 0,
            WeightDemand = 0,
            ReadyTime = 0,
            DueTime = 8 * 60, // 8 hours from start of day
            RequiresRefrigeration = false
        }).ToList();

        // 2. Add the entire collection to the Stops DbSet
        db.Jobs.AddRange(newJobs);

        // 3. Save changes to the database
        db.SaveChanges();
    }
}

