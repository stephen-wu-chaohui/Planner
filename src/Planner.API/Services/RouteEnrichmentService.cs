using Microsoft.EntityFrameworkCore;
using Planner.Contracts.Optimization;
using Planner.Infrastructure.Persistence;
using Planner.Messaging.Optimization.Outputs;

namespace Planner.API.Services;

/// <summary>
/// Service for enriching routing results with database data.
/// </summary>
public interface IRouteEnrichmentService {
    /// <summary>
    /// Enriches an OptimizeRouteResponse with vehicle names, job details, and customer names from the database.
    /// </summary>
    Task<RoutingResultDto> EnrichAsync(OptimizeRouteResponse response);
}

/// <summary>
/// Scoped service for enriching routing results with database lookups.
/// </summary>
public sealed class RouteEnrichmentService(PlannerDbContext db) : IRouteEnrichmentService {
    public async Task<RoutingResultDto> EnrichAsync(OptimizeRouteResponse response) {
        ArgumentNullException.ThrowIfNull(response);

        if (response.Routes.Count == 0) {
            // No routes to enrich, return basic DTO
            return new RoutingResultDto(
                response.TenantId,
                response.OptimizationRunId,
                response.CompletedAt,
                [],
                response.TotalCost,
                response.ErrorMessage);
        }

        // Collect all IDs we need to look up
        var vehicleIds = response.Routes.Select(r => r.VehicleId).Distinct().ToList();
        var locationIds = response.Routes
            .SelectMany(r => r.Stops)
            .Select(s => s.LocationId)
            .Distinct()
            .ToList();

        // Perform database queries
        var vehicles = await db.Vehicles
            .Where(v => vehicleIds.Contains(v.Id))
            .Select(v => new { v.Id, v.Name })
            .ToListAsync();

        var jobs = await db.Jobs
            .Where(j => locationIds.Contains(j.LocationId))
            .Select(j => new { j.LocationId, j.Name, j.JobType, j.CustomerId })
            .ToListAsync();

        var customerIds = jobs.Select(j => j.CustomerId).Distinct().ToList();
        var customers = customerIds.Any()
            ? await db.Customers
                .Where(c => customerIds.Contains(c.CustomerId))
                .Select(c => new { c.CustomerId, c.Name })
                .ToListAsync()
            : [];

        // Create lookup dictionaries
        var vehicleLookup = vehicles.ToDictionary(v => v.Id, v => v.Name);
        var jobLookup = jobs.ToDictionary(j => j.LocationId, j => j);
        var customerLookup = customers.ToDictionary(c => c.CustomerId, c => c.Name);

        // Build enriched DTOs
        var enrichedRoutes = response.Routes.Select(route => {
            vehicleLookup.TryGetValue(route.VehicleId, out var vehicleName);

            var enrichedStops = route.Stops.Select(stop => {
                jobLookup.TryGetValue(stop.LocationId, out var job);
                string? customerName = null;
                if (job != null && customerLookup.TryGetValue(job.CustomerId, out var name)) {
                    customerName = name;
                }

                return new TaskAssignmentDto(
                    stop.LocationId,
                    stop.ArrivalTime,
                    stop.DepartureTime,
                    stop.PalletLoad,
                    stop.WeightLoad,
                    stop.RefrigeratedLoad,
                    job?.Name,
                    job?.JobType.ToString(),
                    customerName
                );
            }).ToList();

            return new RouteDto(
                route.VehicleId,
                enrichedStops,
                route.TotalMinutes,
                route.TotalDistanceKm,
                route.TotalCost,
                vehicleName
            );
        }).ToList();

        return new RoutingResultDto(
            response.TenantId,
            response.OptimizationRunId,
            response.CompletedAt,
            enrichedRoutes,
            response.TotalCost,
            response.ErrorMessage
        );
    }
}
