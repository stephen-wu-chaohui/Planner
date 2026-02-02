using Microsoft.EntityFrameworkCore;
using Planner.Application;
using Planner.Application.Messaging;
using Planner.Contracts.Optimization;
using Planner.Infrastructure.Persistence.Persistence;

namespace Planner.API.Services;

/// <summary>
/// Publishes routing results to connected SignalR clients.
/// </summary>
public interface IRouteService {
    Task PublishAsync(RoutingResultDto result);
}

/// <summary>
/// Scoped service for broadcasting routing results.
/// </summary>
public sealed class RouteService(
    PlannerDbContext db,
    IMessageHubPublisher hub,
    ITenantContext tenantContext) : IRouteService {
    public async Task PublishAsync(RoutingResultDto result) {
        ArgumentNullException.ThrowIfNull(result);
        EnsureTenantContext(result.TenantId);
        var enriched = await EnrichAsync(result);
        await hub.PublishAsync(nameof(RoutingResultDto), enriched);
    }

    private async Task<RoutingResultDto> EnrichAsync(RoutingResultDto result) {
        if (result.Routes.Count == 0) {
            return result;
        }

        var vehicleIds = result.Routes.Select(r => r.VehicleId).Distinct().ToList();
        var locationIds = result.Routes
            .SelectMany(r => r.Stops)
            .Select(s => s.LocationId)
            .Distinct()
            .ToList();

        var vehicles = !vehicleIds.Any()
            ? []
            : await db.Vehicles
                .Where(v => vehicleIds.Contains(v.Id))
                .Select(v => new { v.Id, v.Name })
                .ToListAsync();

        var jobs = !locationIds.Any()
            ? []
            : await db.Jobs
                .Where(j => locationIds.Contains(j.LocationId))
                .Select(j => new { j.LocationId, j.Name, j.JobType, j.CustomerId })
                .ToListAsync();

        var customerIds = jobs.Select(j => j.CustomerId).Distinct().ToList();
        var customers = !customerIds.Any()
            ? []
            : await db.Customers
                .Where(c => customerIds.Contains(c.CustomerId))
                .Select(c => new { c.CustomerId, c.Name })
                .ToListAsync();

        var vehicleLookup = vehicles.ToDictionary(v => v.Id, v => v.Name);
        var jobLookup = jobs.ToDictionary(j => j.LocationId, j => j);
        var customerLookup = customers.ToDictionary(c => c.CustomerId, c => c.Name);

        var enrichedRoutes = result.Routes.Select(route => {
            vehicleLookup.TryGetValue(route.VehicleId, out var vehicleName);
            var enrichedStops = route.Stops.Select(stop => {
                jobLookup.TryGetValue(stop.LocationId, out var job);
                string? customerName = null;
                if (job != null && customerLookup.TryGetValue(job.CustomerId, out var name)) {
                    customerName = name;
                }

                return stop with {
                    JobName = job?.Name,
                    JobType = job?.JobType.ToString(),
                    CustomerName = customerName
                };
            }).ToList();

            return route with {
                VehicleName = vehicleName,
                Stops = enrichedStops
            };
        }).ToList();

        return result with { Routes = enrichedRoutes };
    }

    private void EnsureTenantContext(Guid tenantId) {
        if (!tenantContext.IsSet) {
            tenantContext.SetTenant(tenantId);
            return;
        }

        if (tenantContext.TenantId != tenantId) {
            throw new InvalidOperationException("Tenant mismatch for routing result.");
        }
    }
}
