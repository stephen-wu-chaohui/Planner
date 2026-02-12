using Microsoft.EntityFrameworkCore;
using Planner.API.Mappings;
using Planner.Application;
using Planner.Contracts.API;
using Planner.Infrastructure.Persistence;
using DomainRoute = Planner.Domain.Route;
using HotChocolate.Resolvers;

namespace Planner.API.GraphQL;

public sealed class Query {
    private const string TenantIdKey = "TenantId";

    /// <summary>
    /// Ensures tenant context is set in the resolver scope by retrieving TenantId from GraphQL context.
    /// </summary>
    private static void EnsureTenantContext(IResolverContext context, ITenantContext tenantContext)
    {
        if (tenantContext.IsSet)
            return; // Already set

        if (!context.ContextData.TryGetValue(TenantIdKey, out var tenantIdObj) || tenantIdObj is not Guid tenantId)
        {
            throw new UnauthorizedAccessException("Tenant context is not available.");
        }

        tenantContext.SetTenant(tenantId);
    }

    // Jobs
    public async Task<List<JobDto>> GetJobs(
        IResolverContext context,
        [Service] PlannerDbContext db, 
        [Service] ITenantContext tenantContext) 
    {
        EnsureTenantContext(context, tenantContext);

        return await db.Jobs
            .AsNoTracking()
            .Include(j => j.Location)
            .Select(j => j.ToDto())
            .ToListAsync();
    }

    public async Task<JobDto?> GetJobById(long id, IResolverContext context, [Service] PlannerDbContext db, [Service] ITenantContext tenantContext) {
        EnsureTenantContext(context, tenantContext);

        var entity = await db.Jobs
            .AsNoTracking()
            .Include(j => j.Location)
            .FirstOrDefaultAsync(j => j.Id == id);

        return entity?.ToDto();
    }

    // Customers
    public async Task<List<CustomerDto>> GetCustomers(IResolverContext context, [Service] PlannerDbContext db, [Service] ITenantContext tenantContext) {
        EnsureTenantContext(context, tenantContext);

        return await db.Customers
            .AsNoTracking()
            .Include(c => c.Location)
            .Select(c => c.ToDto())
            .ToListAsync();
    }

    public async Task<CustomerDto?> GetCustomerById(long id, IResolverContext context, [Service] PlannerDbContext db, [Service] ITenantContext tenantContext) {
        EnsureTenantContext(context, tenantContext);

        var entity = await db.Customers
            .AsNoTracking()
            .Include(c => c.Location)
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        return entity?.ToDto();
    }

    // Vehicles
    public async Task<List<VehicleDto>> GetVehicles(IResolverContext context, [Service] PlannerDbContext db, [Service] ITenantContext tenantContext) {
        EnsureTenantContext(context, tenantContext);

        return await db.Set<Planner.Domain.Vehicle>()
            .AsNoTracking()
            .Include(v => v.StartDepot)
            .Include(v => v.EndDepot)
            .Select(v => v.ToDto())
            .ToListAsync();
    }

    public async Task<VehicleDto?> GetVehicleById(long id, IResolverContext context, [Service] PlannerDbContext db, [Service] ITenantContext tenantContext) {
        EnsureTenantContext(context, tenantContext);

        var entity = await db.Set<Planner.Domain.Vehicle>()
            .AsNoTracking()
            .Include(v => v.StartDepot)
            .Include(v => v.EndDepot)
            .FirstOrDefaultAsync(v => v.Id == id);

        return entity?.ToDto();
    }

    // Depots
    public async Task<List<DepotDto>> GetDepots(IResolverContext context, [Service] PlannerDbContext db, [Service] ITenantContext tenantContext) {
        EnsureTenantContext(context, tenantContext);

        return await db.Depots
            .AsNoTracking()
            .Include(d => d.Location)
            .Select(d => d.ToDto())
            .ToListAsync();
    }

    public async Task<DepotDto?> GetDepotById(long id, IResolverContext context, [Service] PlannerDbContext db, [Service] ITenantContext tenantContext) {
        EnsureTenantContext(context, tenantContext);

        var entity = await db.Depots
            .AsNoTracking()
            .Include(d => d.Location)
            .FirstOrDefaultAsync(d => d.Id == id);

        return entity?.ToDto();
    }

    // Locations
    public async Task<List<LocationDto>> GetLocations(IResolverContext context, [Service] PlannerDbContext db, [Service] ITenantContext tenantContext) {
        EnsureTenantContext(context, tenantContext);

        return await db.Locations
            .AsNoTracking()
            .Select(l => l.ToDto())
            .ToListAsync();
    }

    public async Task<LocationDto?> GetLocationById(long id, IResolverContext context, [Service] PlannerDbContext db, [Service] ITenantContext tenantContext) {
        EnsureTenantContext(context, tenantContext);

        var entity = await db.Locations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id);

        return entity?.ToDto();
    }

    // Routes
    public async Task<List<DomainRoute>> GetRoutes(IResolverContext context, [Service] PlannerDbContext db, [Service] ITenantContext tenantContext) {
        EnsureTenantContext(context, tenantContext);

        return await db.Set<DomainRoute>()
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<DomainRoute?> GetRouteById(long id, IResolverContext context, [Service] PlannerDbContext db, [Service] ITenantContext tenantContext) {
        EnsureTenantContext(context, tenantContext);

        return await db.Set<DomainRoute>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    // Tasks
    public async Task<List<Planner.Domain.TaskItem>> GetTasks(IResolverContext context, [Service] PlannerDbContext db, [Service] ITenantContext tenantContext) {
        EnsureTenantContext(context, tenantContext);

        return await db.Tasks
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Planner.Domain.TaskItem?> GetTaskById(long id, IResolverContext context, [Service] PlannerDbContext db, [Service] ITenantContext tenantContext) {
        EnsureTenantContext(context, tenantContext);

        return await db.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    }
}
