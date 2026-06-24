using Microsoft.EntityFrameworkCore;
using Planner.API.Caching;
using Planner.Application.OptimizationRuns;
using Planner.Contracts.OptimizationRuns;
using Planner.Domain;
using Planner.Infrastructure;
using Planner.Messaging.Optimization.Inputs;

namespace Planner.API.Services;

public sealed class OptimizationRunSnapshotBuilder(
    IPlannerDataCenter dataCenter,
    IMatrixCalculationService matrixService) : IOptimizationRunSnapshotBuilder {

    public async Task<OptimizationRunDocument> BuildAsync(
        Guid tenantId,
        string? requestedBy,
        int? searchTimeLimitSeconds,
        CancellationToken ct) {

        await EnsureJobsForAllCustomersAsync(tenantId, ct);

        var jobs = await dataCenter.GetOrFetchAsync(
            CacheKeys.JobsList(tenantId),
            async () => await dataCenter.DbContext.Jobs
                .Include(j => j.Location)
                .ToListAsync(ct)) ?? [];

        var vehicles = await dataCenter.GetOrFetchAsync(
            CacheKeys.VehiclesList(tenantId),
            async () => await dataCenter.DbContext.Vehicles
                .Include(v => v.StartDepot)
                    .ThenInclude(d => d!.Location)
                .Include(v => v.EndDepot)
                    .ThenInclude(d => d!.Location)
                .ToListAsync(ct)) ?? [];

        var routableVehicles = vehicles
            .Where(v =>
                v.StartDepot is not null &&
                v.EndDepot is not null &&
                v.StartDepot.Location is not null &&
                v.EndDepot.Location is not null)
            .ToList();

        var settings = new OptimizationSettings(
            SearchTimeLimitSeconds: searchTimeLimitSeconds ?? (1 * jobs.Count));

        var depotLocations = routableVehicles
            .SelectMany(v => new[] { v.StartDepot!.Location!, v.EndDepot!.Location! })
            .GroupBy(l => l.Id)
            .Select(g => g.First())
            .ToList();

        var jobLocations = jobs.Select(j => j.Location).ToList();
        var allLocations = depotLocations.Concat(jobLocations).ToList();
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

            return ToInput.FromDepotLocation(1);
        }).ToArray();

        var request = new OptimizeRouteRequest(
            TenantId: tenantId,
            OptimizationRunId: Guid.NewGuid(),
            RequestedAt: DateTime.UtcNow,
            Stops: stops,
            Vehicles: routableVehicles.Select(ToInput.FromVehicle).ToArray(),
            DistanceMatrix: distanceMatrix,
            TravelTimeMatrix: travelTimeMatrix,
            Settings: settings);

        var now = request.RequestedAt;
        var summary = new OptimizationRunSummaryDto(
            request.Stops.Length,
            request.Vehicles.Length,
            now,
            settings.SearchTimeLimitSeconds,
            requestedBy);

        return new OptimizationRunDocument(
            Id: request.OptimizationRunId.ToString(),
            TenantId: tenantId,
            OptimizationRunId: request.OptimizationRunId,
            SchemaVersion: 1,
            Version: 1,
            Status: OptimizationRunStatus.Created,
            RequestedAtUtc: now,
            UpdatedAtUtc: now,
            RequestSnapshot: request,
            Summary: summary,
            SolverResult: null,
            AiInsight: null,
            Timeline: [
                new OptimizationRunTimelineEventDto(
                    Guid.NewGuid(),
                    OptimizationRunStatus.Created,
                    now,
                    "Optimization run created.")
            ],
            Attempts: [],
            ErrorMessage: null);
    }

    private async Task EnsureJobsForAllCustomersAsync(Guid tenantId, CancellationToken ct) {
        var jobsWithNoCustomers = await dataCenter.DbContext.Jobs
            .Where(j => !dataCenter.DbContext.Customers.Any(c => c.CustomerId == j.CustomerId))
            .ToListAsync(ct);
        dataCenter.DbContext.Jobs.RemoveRange(jobsWithNoCustomers);

        var customersWithNoJobs = await dataCenter.DbContext.Customers
            .Where(c => !dataCenter.DbContext.Jobs.Any(j => j.CustomerId == c.CustomerId))
            .ToListAsync(ct);

        var newJobs = customersWithNoJobs.Select(c => new Job {
            CustomerId = c.CustomerId,
            TenantId = tenantId,
            Name = $"Job for {c.Name}",
            LocationId = c.LocationId,
            Location = c.Location,
            JobType = JobType.Delivery,
            ServiceTimeMinutes = c.DefaultServiceMinutes,
            PalletDemand = 0,
            WeightDemand = 0,
            ReadyTime = 0,
            DueTime = 8 * 60,
            RequiresRefrigeration = false
        }).ToList();

        dataCenter.DbContext.Jobs.AddRange(newJobs);
        await dataCenter.DbContext.SaveChangesAsync(ct);

        await dataCenter.RemoveCacheKeysAsync(
            ct,
            CacheKeys.JobsList(tenantId),
            CacheKeys.CustomersList(tenantId));
    }
}
