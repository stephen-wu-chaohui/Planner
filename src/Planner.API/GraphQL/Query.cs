using Microsoft.EntityFrameworkCore;
using Planner.API.Mappings;
using Planner.Application;
using Planner.Contracts.API;
using Planner.Infrastructure.Persistence;
using DomainRoute = Planner.Domain.Route;

namespace Planner.API.GraphQL;

public sealed class Query {
    // Jobs
    public async Task<List<JobDto>> GetJobs([Service] PlannerDbContext db) {
        return await db.Jobs
            .AsNoTracking()
            .Include(j => j.Location)
            .Select(j => j.ToDto())
            .ToListAsync();
    }

    public async Task<JobDto?> GetJobById(long id, [Service] PlannerDbContext db) {
        var entity = await db.Jobs
            .AsNoTracking()
            .Include(j => j.Location)
            .FirstOrDefaultAsync(j => j.Id == id);

        return entity?.ToDto();
    }

    // Customers
    public async Task<List<CustomerDto>> GetCustomers([Service] PlannerDbContext db) {
        return await db.Customers
            .AsNoTracking()
            .Include(c => c.Location)
            .Select(c => c.ToDto())
            .ToListAsync();
    }

    public async Task<CustomerDto?> GetCustomerById(long id, [Service] PlannerDbContext db) {
        var entity = await db.Customers
            .AsNoTracking()
            .Include(c => c.Location)
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        return entity?.ToDto();
    }

    // Vehicles
    public async Task<List<VehicleDto>> GetVehicles([Service] PlannerDbContext db) {
        return await db.Set<Planner.Domain.Vehicle>()
            .AsNoTracking()
            .Include(v => v.StartDepot)
            .Include(v => v.EndDepot)
            .Select(v => v.ToDto())
            .ToListAsync();
    }

    public async Task<VehicleDto?> GetVehicleById(long id, [Service] PlannerDbContext db) {
        var entity = await db.Set<Planner.Domain.Vehicle>()
            .AsNoTracking()
            .Include(v => v.StartDepot)
            .Include(v => v.EndDepot)
            .FirstOrDefaultAsync(v => v.Id == id);

        return entity?.ToDto();
    }

    // Depots
    public async Task<List<DepotDto>> GetDepots([Service] PlannerDbContext db) {
        return await db.Depots
            .AsNoTracking()
            .Include(d => d.Location)
            .Select(d => d.ToDto())
            .ToListAsync();
    }

    public async Task<DepotDto?> GetDepotById(long id, [Service] PlannerDbContext db) {
        var entity = await db.Depots
            .AsNoTracking()
            .Include(d => d.Location)
            .FirstOrDefaultAsync(d => d.Id == id);

        return entity?.ToDto();
    }

    // Locations
    public async Task<List<LocationDto>> GetLocations([Service] PlannerDbContext db) {
        return await db.Locations
            .AsNoTracking()
            .Select(l => l.ToDto())
            .ToListAsync();
    }

    public async Task<LocationDto?> GetLocationById(long id, [Service] PlannerDbContext db) {
        var entity = await db.Locations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id);

        return entity?.ToDto();
    }

    // Routes
    public async Task<List<DomainRoute>> GetRoutes([Service] PlannerDbContext db) {
        return await db.Set<DomainRoute>()
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<DomainRoute?> GetRouteById(long id, [Service] PlannerDbContext db) {
        return await db.Set<DomainRoute>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    // Tasks
    public async Task<List<Planner.Domain.TaskItem>> GetTasks([Service] PlannerDbContext db) {
        return await db.Tasks
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Planner.Domain.TaskItem?> GetTaskById(long id, [Service] PlannerDbContext db) {
        return await db.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    }
}
