using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Planner.Domain;

namespace Planner.Infrastructure.Persistence;

public interface IPlannerDbContext {
    DbSet<Tenant> Tenants { get; }
    DbSet<User> Users { get; }
    DbSet<SystemEvent> SystemEvents { get; }
    DbSet<Job> Jobs { get; }
    DbSet<TaskItem> Tasks { get; }
    DbSet<Vehicle> Vehicles { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Location> Locations { get; }
    DbSet<Depot> Depots { get; }

    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    int SaveChanges();
}
