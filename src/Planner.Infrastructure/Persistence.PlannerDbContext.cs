using Microsoft.EntityFrameworkCore;
using Planner.Application;
using Planner.Domain;

namespace Planner.Infrastructure.Persistence;

public class PlannerDbContext(DbContextOptions<PlannerDbContext> options, ITenantContext tenant) : DbContext(options) {
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<SystemEvent> SystemEvents => Set<SystemEvent>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Depot> Depots => Set<Depot>();


    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Vehicle>()
            .HasQueryFilter(v => v.TenantId == tenant.TenantId);
        modelBuilder.Entity<Job>()
            .HasQueryFilter(v => v.TenantId == tenant.TenantId);
        modelBuilder.Entity<UserAccount>()
            .HasQueryFilter(v => v.TenantId == tenant.TenantId);
        modelBuilder.Entity<Customer>()
            .HasQueryFilter(v => v.TenantId == tenant.TenantId);
        modelBuilder.Entity<Depot>()
            .HasQueryFilter(v => v.TenantId == tenant.TenantId);
    }
}
