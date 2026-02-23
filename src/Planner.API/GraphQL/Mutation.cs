using Microsoft.EntityFrameworkCore;
using Planner.API.Mappings;
using Planner.Application;
using Planner.Contracts.API;
using Planner.Domain;
using Planner.Infrastructure;

namespace Planner.API.GraphQL;

public sealed class Mutation {
    // Job Mutations
    public async Task<JobDto> CreateJob(JobDto input, [Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenant) {

        var entity = input.ToDomain(tenant.TenantId);
        dataCenter.DbContext.Jobs.Add(entity);
        await dataCenter.DbContext.SaveChangesAsync();
        return entity.ToDto();
    }

    public async Task<JobDto?> UpdateJob(long id, JobDto input, [Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenant) {

        if (id != input.Id) {
            throw new ArgumentException($"Job ID in path ({id}) does not match ID in request body ({input.Id})");
        }

        var existing = await dataCenter.DbContext.Jobs.Where(j => j.TenantId == tenant.TenantId).FirstOrDefaultAsync(j => j.Id == id);
        if (existing == null) {
            return null;
        }

        var updated = input.ToDomain(tenant.TenantId);
        dataCenter.DbContext.Entry(existing).CurrentValues.SetValues(updated);
        await dataCenter.DbContext.SaveChangesAsync();

        return existing.ToDto();
    }

    public async Task<bool> DeleteJob(long id, [Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenant) {
        var entity = await dataCenter.DbContext.Jobs.Where(j => j.TenantId == tenant.TenantId).FirstOrDefaultAsync(j => j.Id == id);
        if (entity is null)
            return false;

        dataCenter.DbContext.Jobs.Remove(entity);
        await dataCenter.DbContext.SaveChangesAsync();
        return true;
    }

    // Customer Mutations
    public async Task<CustomerDto> CreateCustomer(CustomerDto input, [Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenant) {
        var entity = input.ToDomain(tenant.TenantId);
        dataCenter.DbContext.Customers.Add(entity);
        await dataCenter.DbContext.SaveChangesAsync();
        return entity.ToDto();
    }

    public async Task<CustomerDto?> UpdateCustomer(long id, CustomerDto input, [Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenant) {
        if (id != input.CustomerId) {
            throw new ArgumentException($"Customer ID in path ({id}) does not match ID in request body ({input.CustomerId})");
        }

        var existing = await dataCenter.DbContext.Customers.Where(c => c.TenantId == tenant.TenantId).FirstOrDefaultAsync(c => c.CustomerId == id);
        if (existing == null) {
            return null;
        }

        var updated = input.ToDomain(tenant.TenantId);
        dataCenter.DbContext.Entry(existing).CurrentValues.SetValues(updated);
        await dataCenter.DbContext.SaveChangesAsync();

        return existing.ToDto();
    }

    public async Task<bool> DeleteCustomer(long id, [Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenant) {
        var entity = await dataCenter.DbContext.Customers.Where(c => c.TenantId == tenant.TenantId).FirstOrDefaultAsync(c => c.CustomerId == id);
        if (entity is null)
            return false;

        dataCenter.DbContext.Customers.Remove(entity);
        await dataCenter.DbContext.SaveChangesAsync();
        return true;
    }

    // Vehicle Mutations
    public async Task<VehicleDto> CreateVehicle(VehicleDto input, [Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenant) {
        var entity = input.ToDomain(tenant.TenantId);
        dataCenter.DbContext.Set<Planner.Domain.Vehicle>().Add(entity);
        await dataCenter.DbContext.SaveChangesAsync();
        return entity.ToDto();
    }

    public async Task<VehicleDto?> UpdateVehicle(long id, VehicleDto input, [Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenant) {
        if (id != input.Id) {
            throw new ArgumentException($"Vehicle ID in path ({id}) does not match ID in request body ({input.Id})");
        }

        var existing = await dataCenter.DbContext.Set<Planner.Domain.Vehicle>().Where(v => v.TenantId == tenant.TenantId).FirstOrDefaultAsync(v => v.Id == id);
        if (existing == null) {
            return null;
        }

        var updated = input.ToDomain(tenant.TenantId);
        dataCenter.DbContext.Entry(existing).CurrentValues.SetValues(updated);
        await dataCenter.DbContext.SaveChangesAsync();

        return existing.ToDto();
    }

    public async Task<bool> DeleteVehicle(long id, [Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenantContext) {
        var entity = await dataCenter.DbContext.Set<Planner.Domain.Vehicle>().Where(v => v.TenantId == tenantContext.TenantId).FirstOrDefaultAsync(v => v.Id == id);
        if (entity is null)
            return false;

        dataCenter.DbContext.Set<Planner.Domain.Vehicle>().Remove(entity);
        await dataCenter.DbContext.SaveChangesAsync();
        return true;
    }

    // Depot Mutations
    public async Task<DepotDto> CreateDepot(DepotDto input, [Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenant) {
        var entity = input.ToDomain(tenant.TenantId);
        dataCenter.DbContext.Depots.Add(entity);
        await dataCenter.DbContext.SaveChangesAsync();
        return entity.ToDto();
    }

    public async Task<DepotDto?> UpdateDepot(long id, DepotDto input, [Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenant) {
        if (id != input.Id) {
            throw new ArgumentException($"Depot ID in path ({id}) does not match ID in request body ({input.Id})");
        }

        var existing = await dataCenter.DbContext.Depots.Where(d => d.TenantId == tenant.TenantId).FirstOrDefaultAsync(d => d.Id == id);
        if (existing is null) {
            return null;
        }

        var updated = input.ToDomain(tenant.TenantId);
        dataCenter.DbContext.Entry(existing).CurrentValues.SetValues(updated);
        await dataCenter.DbContext.SaveChangesAsync();

        return existing.ToDto();
    }

    public async Task<bool> DeleteDepot(long id, [Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenantContext) {
        var entity = await dataCenter.DbContext.Depots.Where(d => d.TenantId == tenantContext.TenantId).FirstOrDefaultAsync(d => d.Id == id);
        if (entity is null)
            return false;

        dataCenter.DbContext.Depots.Remove(entity);
        await dataCenter.DbContext.SaveChangesAsync();
        return true;
    }

    // Location Mutations
    public async Task<LocationDto> CreateLocation(LocationDto input, [Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenantContext) {
        var entity = input.ToDomain();
        dataCenter.DbContext.Locations.Add(entity);
        await dataCenter.DbContext.SaveChangesAsync();
        return entity.ToDto();
    }

    public async Task<LocationDto?> UpdateLocation(long id, LocationDto input, [Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenantContext) {
        if (id != input.Id) {
            throw new ArgumentException($"Location ID in path ({id}) does not match ID in request body ({input.Id})");
        }

        var existing = await dataCenter.DbContext.Locations.FindAsync(id);
        if (existing == null) {
            return null;
        }

        var updated = input.ToDomain();
        dataCenter.DbContext.Entry(existing).CurrentValues.SetValues(updated);
        await dataCenter.DbContext.SaveChangesAsync();

        return existing.ToDto();
    }

    public async Task<bool> DeleteLocation(long id, [Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenantContext) {
        var entity = await dataCenter.DbContext.Locations.FirstOrDefaultAsync(l => l.Id == id);
        if (entity is null)
            return false;

        dataCenter.DbContext.Locations.Remove(entity);
        await dataCenter.DbContext.SaveChangesAsync();
        return true;
    }

    // Task Mutations
    public async Task<TaskItem> CreateTask(TaskItem input, [Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenant) {
        dataCenter.DbContext.Tasks.Add(input);
        await dataCenter.DbContext.SaveChangesAsync();
        return input;
    }

    public async Task<TaskItem?> UpdateTask(long id, TaskItem input, [Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenant) {
        if (id != input.Id) {
            throw new ArgumentException($"Task ID in path ({id}) does not match ID in request body ({input.Id})");
        }

        var existing = await dataCenter.DbContext.Tasks.FindAsync(id);
        if (existing == null) {
            return null;
        }

        dataCenter.DbContext.Entry(existing).CurrentValues.SetValues(input);
        await dataCenter.DbContext.SaveChangesAsync();

        return existing;
    }

    public async Task<bool> DeleteTask(long id, [Service] IPlannerDataCenter dataCenter, [Service] ITenantContext tenantContext) {
        var entity = await dataCenter.DbContext.Tasks.FirstOrDefaultAsync(t => t.Id == id);
        if (entity is null)
            return false;

        dataCenter.DbContext.Tasks.Remove(entity);
        await dataCenter.DbContext.SaveChangesAsync();
        return true;
    }
}
