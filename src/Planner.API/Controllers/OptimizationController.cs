using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.API.Services;
using Planner.Application;
using Planner.Domain;
using Planner.Infrastructure.Persistence;
using Planner.Messaging;
using Planner.Messaging.Messaging;
using Planner.Messaging.Optimization;
using Planner.Messaging.Optimization.Requests;
using Planner.API.Services;

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

        if (request.Jobs.Count == 0 || request.Vehicles.Count == 0)
            return BadRequest("No jobs or vehicles available for optimization.");

        await bus.PublishAsync(MessageRoutes.Request, request);
        return Ok(request.Settings);
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
        var (distanceMatrix, travelTimeMatrix) = MatrixBuilder.BuildMatrices(allLocations, settings);

        return new OptimizeRouteRequest(
            tenant.TenantId,
            OptimizationRunId: Guid.NewGuid(),
            RequestedAt: DateTime.UtcNow,
            Jobs: jobs.Select(ToInput.ToJobInput).ToList(),
            Vehicles: vehicles.Select(ToInput.ToVehicleInput).ToList(),
            DistanceMatrix: distanceMatrix,
            TravelTimeMatrix: travelTimeMatrix,
            Settings: settings
        );
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

        // 2. Add the entire collection to the Jobs DbSet
        db.Jobs.AddRange(newJobs);

        // 3. Save changes to the database
        db.SaveChanges();
    }
}

