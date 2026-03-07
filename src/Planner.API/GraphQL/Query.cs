using Microsoft.EntityFrameworkCore;
using Planner.API.Caching;
using Planner.API.Mappings;
using Planner.Application;
using Planner.Contracts.API;
using Planner.Domain;
using DomainRoute = Planner.Domain.Route;
using Planner.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace Planner.API.GraphQL;

public sealed class Query {
    // Jobs
    [Authorize]
    public async Task<List<JobDto>> GetJobs(
        [Service] IPlannerDataCenter dataCenter,
        [Service] ITenantContext tenantContext) {

        return await dataCenter.GetOrFetchAsync(
            CacheKeys.JobsList(tenantContext.TenantId),
            async () => await dataCenter.DbContext.Jobs
                .AsNoTracking()
                .Where(j => j.TenantId == tenantContext.TenantId)
                .Include(j => j.Location)
                .Select(j => j.ToDto())
                .ToListAsync()) ?? [];
    }

    [Authorize]
    public async Task<JobDto?> GetJobById(long id, [Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenantContext) {
        return await dataCenter.GetOrFetchAsync(
            CacheKeys.JobById(id, tenantContext.TenantId),
            async () => await dataCenter.DbContext.Jobs
                .AsNoTracking()
                .Where(j => j.TenantId == tenantContext.TenantId)
                .Include(j => j.Location)
                .Where(j => j.Id == id)
                .Select(j => j.ToDto())
                .FirstOrDefaultAsync());
    }

    // Customers
    [Authorize]
    public async Task<List<CustomerDto>> GetCustomers([Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenantContext) {
        return await dataCenter.GetOrFetchAsync(
            CacheKeys.CustomersList(tenantContext.TenantId),
            async () => await dataCenter.DbContext.Customers
                .AsNoTracking()
                .Where(c => c.TenantId == tenantContext.TenantId)
                .Include(c => c.Location)
                .Select(c => c.ToDto())
                .ToListAsync()) ?? [];
    }

    [Authorize]
    public async Task<CustomerDto?> GetCustomerById(long id, [Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenantContext) {
        return await dataCenter.GetOrFetchAsync(
            CacheKeys.CustomerById(id, tenantContext.TenantId),
            async () => await dataCenter.DbContext.Customers
                .AsNoTracking()
                .Where(c => c.TenantId == tenantContext.TenantId)
                .Include(c => c.Location)
                .Where(c => c.CustomerId == id)
                .Select(c => c.ToDto())
                .FirstOrDefaultAsync());
    }

    // Vehicles
    [Authorize]
    public async Task<List<VehicleDto>> GetVehicles([Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenantContext) {
        return await dataCenter.GetOrFetchAsync(
            CacheKeys.VehiclesList(tenantContext.TenantId),
            async () => await dataCenter.DbContext.Set<Planner.Domain.Vehicle>()
                .AsNoTracking()
                .Where(v => v.TenantId == tenantContext.TenantId)
                .Include(v => v.StartDepot)
                .Include(v => v.EndDepot)
                .Select(v => v.ToDto())
                .ToListAsync()) ?? [];
    }

    [Authorize]
    public async Task<VehicleDto?> GetVehicleById(long id, [Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenantContext) {
        return await dataCenter.GetOrFetchAsync(
            CacheKeys.VehicleById(id, tenantContext.TenantId),
            async () => await dataCenter.DbContext.Set<Planner.Domain.Vehicle>()
                .AsNoTracking()
                .Where(v => v.TenantId == tenantContext.TenantId)
                .Include(v => v.StartDepot)
                .Include(v => v.EndDepot)
                .Where(v => v.Id == id)
                .Select(v => v.ToDto())
                .FirstOrDefaultAsync());
    }

    // Depots
    [Authorize]
    public async Task<List<DepotDto>> GetDepots([Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenantContext) {
        return await dataCenter.GetOrFetchAsync(
            CacheKeys.DepotsList(tenantContext.TenantId),
            async () => await dataCenter.DbContext.Depots
                .AsNoTracking()
                .Where(d => d.TenantId == tenantContext.TenantId)
                .Include(d => d.Location)
                .Select(d => d.ToDto())
                .ToListAsync()) ?? [];
    }

    [Authorize]
    public async Task<DepotDto?> GetDepotById(long id, [Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenantContext) {

        return await dataCenter.GetOrFetchAsync(
            CacheKeys.DepotById(id, tenantContext.TenantId),
            async () => await dataCenter.DbContext.Depots
                .AsNoTracking()
                .Where(d => d.TenantId == tenantContext.TenantId)
                .Include(d => d.Location)
                .Where(d => d.Id == id)
                .Select(d => d.ToDto())
                .FirstOrDefaultAsync());
    }

    // Locations
    public async Task<List<LocationDto>> GetLocations([Service] IPlannerDataCenter dataCenter) {

        return await dataCenter.GetOrFetchAsync(
            CacheKeys.LocationsList(),
            async () => await dataCenter.DbContext.Locations
                .AsNoTracking()
                .Select(l => l.ToDto())
                .ToListAsync()) ?? [];
    }

    public async Task<LocationDto?> GetLocationById(long id, [Service] IPlannerDataCenter dataCenter) {
        return await dataCenter.GetOrFetchAsync(
            CacheKeys.LocationById(id),
            async () => await dataCenter.DbContext.Locations
                .AsNoTracking()
                .Where(l => l.Id == id)
                .Select(l => l.ToDto())
                .FirstOrDefaultAsync());
    }

    // Routes
    [Authorize]
    public async Task<List<DomainRoute>> GetRoutes([Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenantContext) {
        return await dataCenter.GetOrFetchAsync(
            CacheKeys.RoutesList(tenantContext.TenantId),
            async () => await dataCenter.DbContext.Set<DomainRoute>()
                .AsNoTracking()
                .Where(r => r.TenantId == tenantContext.TenantId)
                .ToListAsync()) ?? [];
    }

    [Authorize]
    public async Task<DomainRoute?> GetRouteById(long id, [Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenantContext) {
        return await dataCenter.GetOrFetchAsync(
            CacheKeys.RouteById(id, tenantContext.TenantId),
            async () => await dataCenter.DbContext.Set<DomainRoute>()
                .AsNoTracking()
                .Where(r => r.TenantId == tenantContext.TenantId)
                .FirstOrDefaultAsync(r => r.Id == id));
    }

    // Tasks
    public async Task<List<TaskItem>> GetTasks([Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenantContext) {
        return await dataCenter.GetOrFetchAsync(
            CacheKeys.TasksList(tenantContext.TenantId),
            async () => await dataCenter.DbContext.Tasks
                .AsNoTracking()
                .Where(t => t.TenantId == tenantContext.TenantId)
                .ToListAsync()) ?? [];
    }

    public async Task<TaskItem?> GetTaskById(long id, [Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenantContext) {
        return await dataCenter.GetOrFetchAsync(
            CacheKeys.TaskById(id, tenantContext.TenantId),
            async () => await dataCenter.DbContext.Tasks
                .AsNoTracking()
                .Where(t => t.TenantId == tenantContext.TenantId)
                .FirstOrDefaultAsync(t => t.Id == id));
    }
}
