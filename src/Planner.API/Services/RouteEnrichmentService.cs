using Microsoft.EntityFrameworkCore;
using Planner.Application;
using Planner.Contracts.Optimization;
using Planner.Infrastructure.Persistence;
using Planner.Messaging.Optimization.Outputs;
using System.Linq;

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
public sealed class RouteEnrichmentService(IPlannerDbContext db, ITenantContext tenantContext) : IRouteEnrichmentService {
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
        var vehicles = vehicleIds.Count != 0
            ? await db.Vehicles
                .Where(v => v.TenantId == tenantContext.TenantId)
                .Where(v => vehicleIds.Contains(v.Id))
                .Select(v => new { v.Id, v.Name })
                .ToListAsync()
            : [];

        var jobs = locationIds.Count != 0
            ? await db.Jobs
                .Where(j => j.TenantId == tenantContext.TenantId)
                .Where(j => locationIds.Contains(j.LocationId))
                .Include(j => j.Location) // Ensure Location is included for any necessary data
                .Select(j => new { j.LocationId, j.Name, j.JobType, j.CustomerId, j.Location.Latitude, j.Location.Longitude })
                .ToListAsync()
            : [];

        var customerIds = jobs.Select(j => j.CustomerId).Distinct().ToList();
        var customers = customerIds.Count != 0
            ? await db.Customers.Where(v => v.TenantId == tenantContext.TenantId)
                .Where(c => customerIds.Contains(c.CustomerId))
                .Select(c => new { c.CustomerId, c.Name })
                .ToListAsync()
            : [];

        // Create lookup dictionaries
        var vehicleLookup = vehicles.ToDictionary(v => v.Id, v => v.Name);
        // LocationId is the primary key for jobs, ensuring uniqueness
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
                    job?.Latitude ?? 0,
                    job?.Longitude ?? 0,
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
