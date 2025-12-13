using Microsoft.EntityFrameworkCore;
using Planner.Domain.Entities;

namespace Planner.Infrastructure.Persistence;

public class PlannerDbContext(DbContextOptions<PlannerDbContext> options) : DbContext(options) {
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<SystemEvent> SystemEvents => Set<SystemEvent>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

}
