using Microsoft.EntityFrameworkCore;
using Planner.Application;
using Planner.Domain;
using Planner.Domain.Entities;

namespace Planner.Infrastructure.Persistence;

public class PlannerDbContext(DbContextOptions<PlannerDbContext> options, ITenantContext tenant) : DbContext(options) {
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<SystemEvent> SystemEvents => Set<SystemEvent>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Vehicle>()
            .HasQueryFilter(v => v.TenantId == tenant.TenantId);
    }
}
