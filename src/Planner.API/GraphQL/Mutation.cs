using Microsoft.EntityFrameworkCore;
using Planner.API.Mappings;
using Planner.Application;
using Planner.Contracts.API;
using Planner.Infrastructure.Persistence;
using HotChocolate.Resolvers;

namespace Planner.API.GraphQL;

public sealed class Mutation {
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

    // Job Mutations
    public async Task<JobDto> CreateJob(JobDto input, IResolverContext context, [Service] PlannerDbContext db, [Service] ITenantContext tenant) {
        EnsureTenantContext(context, tenant);
        var entity = input.ToDomain(tenant.TenantId);
        db.Jobs.Add(entity);
        await db.SaveChangesAsync();
        return entity.ToDto();
    }

    public async Task<JobDto?> UpdateJob(long id, JobDto input, IResolverContext context, [Service] PlannerDbContext db, [Service] ITenantContext tenant) {
        EnsureTenantContext(context, tenant);
        if (id != input.Id) {
            throw new ArgumentException($"Job ID in path ({id}) does not match ID in request body ({input.Id})");
        }

        var existing = await db.Jobs.FindAsync(id);
        if (existing == null) {
            return null;
        }

        var updated = input.ToDomain(tenant.TenantId);
        db.Entry(existing).CurrentValues.SetValues(updated);
        await db.SaveChangesAsync();

        return existing.ToDto();
    }

    public async Task<bool> DeleteJob(long id, IResolverContext context, [Service] PlannerDbContext db, [Service] ITenantContext tenantContext) {
        EnsureTenantContext(context, tenantContext);

        var entity = await db.Jobs.FirstOrDefaultAsync(j => j.Id == id);
        if (entity is null)
            return false;

        db.Jobs.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    // Customer Mutations
    public async Task<CustomerDto> CreateCustomer(CustomerDto input, [Service] PlannerDbContext db, [Service] ITenantContext tenant) {
        var entity = input.ToDomain(tenant.TenantId);
        db.Customers.Add(entity);
        await db.SaveChangesAsync();
        return entity.ToDto();
    }

    public async Task<CustomerDto?> UpdateCustomer(long id, CustomerDto input, [Service] PlannerDbContext db, [Service] ITenantContext tenant) {
        if (id != input.CustomerId) {
            throw new ArgumentException($"Customer ID in path ({id}) does not match ID in request body ({input.CustomerId})");
        }

        var existing = await db.Customers.FindAsync(id);
        if (existing == null) {
            return null;
        }

        var updated = input.ToDomain(tenant.TenantId);
        db.Entry(existing).CurrentValues.SetValues(updated);
        await db.SaveChangesAsync();

        return existing.ToDto();
    }

    public async Task<bool> DeleteCustomer(long id, [Service] PlannerDbContext db, [Service] ITenantContext tenantContext) {
        if (!tenantContext.IsSet) {
            throw new UnauthorizedAccessException("Tenant context is not set. Ensure the request is authenticated with a valid tenant_id claim.");
        }

        var entity = await db.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);
        if (entity is null)
            return false;

        db.Customers.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    // Vehicle Mutations
    public async Task<VehicleDto> CreateVehicle(VehicleDto input, [Service] PlannerDbContext db, [Service] ITenantContext tenant) {
        var entity = input.ToDomain(tenant.TenantId);
        db.Set<Planner.Domain.Vehicle>().Add(entity);
        await db.SaveChangesAsync();
        return entity.ToDto();
    }

    public async Task<VehicleDto?> UpdateVehicle(long id, VehicleDto input, [Service] PlannerDbContext db, [Service] ITenantContext tenant) {
        if (id != input.Id) {
            throw new ArgumentException($"Vehicle ID in path ({id}) does not match ID in request body ({input.Id})");
        }

        var existing = await db.Set<Planner.Domain.Vehicle>().FindAsync(id);
        if (existing == null) {
            return null;
        }

        var updated = input.ToDomain(tenant.TenantId);
        db.Entry(existing).CurrentValues.SetValues(updated);
        await db.SaveChangesAsync();

        return existing.ToDto();
    }

    public async Task<bool> DeleteVehicle(long id, [Service] PlannerDbContext db, [Service] ITenantContext tenantContext) {
        if (!tenantContext.IsSet) {
            throw new UnauthorizedAccessException("Tenant context is not set. Ensure the request is authenticated with a valid tenant_id claim.");
        }

        var entity = await db.Set<Planner.Domain.Vehicle>().FirstOrDefaultAsync(v => v.Id == id);
        if (entity is null)
            return false;

        db.Set<Planner.Domain.Vehicle>().Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    // Depot Mutations
    public async Task<DepotDto> CreateDepot(DepotDto input, [Service] PlannerDbContext db, [Service] ITenantContext tenant) {
        var entity = input.ToDomain(tenant.TenantId);
        db.Depots.Add(entity);
        await db.SaveChangesAsync();
        return entity.ToDto();
    }

    public async Task<DepotDto?> UpdateDepot(long id, DepotDto input, [Service] PlannerDbContext db, [Service] ITenantContext tenant) {
        if (id != input.Id) {
            throw new ArgumentException($"Depot ID in path ({id}) does not match ID in request body ({input.Id})");
        }

        var existing = await db.Depots.FindAsync(id);
        if (existing == null) {
            return null;
        }

        var updated = input.ToDomain(tenant.TenantId);
        db.Entry(existing).CurrentValues.SetValues(updated);
        await db.SaveChangesAsync();

        return existing.ToDto();
    }

    public async Task<bool> DeleteDepot(long id, [Service] PlannerDbContext db, [Service] ITenantContext tenantContext) {
        if (!tenantContext.IsSet) {
            throw new UnauthorizedAccessException("Tenant context is not set. Ensure the request is authenticated with a valid tenant_id claim.");
        }

        var entity = await db.Depots.FirstOrDefaultAsync(d => d.Id == id);
        if (entity is null)
            return false;

        db.Depots.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    // Location Mutations
    public async Task<LocationDto> CreateLocation(LocationDto input, [Service] PlannerDbContext db, [Service] ITenantContext tenantContext) {
        // Note: Locations are not tenant-scoped in the current schema,
        // but we still check authentication for consistency
        if (!tenantContext.IsSet) {
            throw new UnauthorizedAccessException("Tenant context is not set. Ensure the request is authenticated with a valid tenant_id claim.");
        }

        var entity = input.ToDomain();
        db.Locations.Add(entity);
        await db.SaveChangesAsync();
        return entity.ToDto();
    }

    public async Task<LocationDto?> UpdateLocation(long id, LocationDto input, [Service] PlannerDbContext db, [Service] ITenantContext tenantContext) {
        // Note: Locations are not tenant-scoped in the current schema,
        // but we still check authentication for consistency
        if (!tenantContext.IsSet) {
            throw new UnauthorizedAccessException("Tenant context is not set. Ensure the request is authenticated with a valid tenant_id claim.");
        }

        if (id != input.Id) {
            throw new ArgumentException($"Location ID in path ({id}) does not match ID in request body ({input.Id})");
        }

        var existing = await db.Locations.FindAsync(id);
        if (existing == null) {
            return null;
        }

        var updated = input.ToDomain();
        db.Entry(existing).CurrentValues.SetValues(updated);
        await db.SaveChangesAsync();

        return existing.ToDto();
    }

    public async Task<bool> DeleteLocation(long id, [Service] PlannerDbContext db, [Service] ITenantContext tenantContext) {
        // Note: Locations are not tenant-scoped in the current schema,
        // but we still check authentication for consistency
        if (!tenantContext.IsSet) {
            throw new UnauthorizedAccessException("Tenant context is not set. Ensure the request is authenticated with a valid tenant_id claim.");
        }

        var entity = await db.Locations.FirstOrDefaultAsync(l => l.Id == id);
        if (entity is null)
            return false;

        db.Locations.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    // Task Mutations
    public async Task<Planner.Domain.TaskItem> CreateTask(Planner.Domain.TaskItem input, [Service] PlannerDbContext db, [Service] ITenantContext tenantContext) {
        // Note: Tasks are not tenant-scoped in the current schema,
        // but we still check authentication for consistency
        if (!tenantContext.IsSet) {
            throw new UnauthorizedAccessException("Tenant context is not set. Ensure the request is authenticated with a valid tenant_id claim.");
        }

        db.Tasks.Add(input);
        await db.SaveChangesAsync();
        return input;
    }

    public async Task<Planner.Domain.TaskItem?> UpdateTask(long id, Planner.Domain.TaskItem input, [Service] PlannerDbContext db, [Service] ITenantContext tenantContext) {
        // Note: Tasks are not tenant-scoped in the current schema,
        // but we still check authentication for consistency
        if (!tenantContext.IsSet) {
            throw new UnauthorizedAccessException("Tenant context is not set. Ensure the request is authenticated with a valid tenant_id claim.");
        }

        if (id != input.Id) {
            throw new ArgumentException($"Task ID in path ({id}) does not match ID in request body ({input.Id})");
        }

        var existing = await db.Tasks.FindAsync(id);
        if (existing == null) {
            return null;
        }

        db.Entry(existing).CurrentValues.SetValues(input);
        await db.SaveChangesAsync();

        return existing;
    }

    public async Task<bool> DeleteTask(long id, [Service] PlannerDbContext db, [Service] ITenantContext tenantContext) {
        // Note: Tasks are not tenant-scoped in the current schema,
        // but we still check authentication for consistency
        if (!tenantContext.IsSet) {
            throw new UnauthorizedAccessException("Tenant context is not set. Ensure the request is authenticated with a valid tenant_id claim.");
        }

        var entity = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id);
        if (entity is null)
            return false;

        db.Tasks.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }
}
