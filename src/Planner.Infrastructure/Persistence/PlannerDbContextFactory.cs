using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Planner.Application;

namespace Planner.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for PlannerDbContext to support EF Core migrations and tooling.
/// This allows EF Core tools to create the DbContext without running the full application.
/// </summary>
public class PlannerDbContextFactory : IDesignTimeDbContextFactory<PlannerDbContext>
{
    public PlannerDbContext CreateDbContext(string[] args)
    {
        var currentDir = Directory.GetCurrentDirectory();

        // Build configuration to get the connection string
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(currentDir)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables();

        var configuration = configBuilder.Build();

        // Get connection string
        var connectionString = configuration.GetConnectionString("PlannerDb");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'PlannerDb' not found in design-time configuration. " +
                $"Ensure shared.appsettings.json or appsettings.json contains the connection string. " +
                $"Current directory: {currentDir}");
        }

        // Configure DbContext options
        var optionsBuilder = new DbContextOptionsBuilder<PlannerDbContext>();
        optionsBuilder
            .UseSqlServer(connectionString)
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging();

        // Create a design-time tenant context (using a default/dummy tenant ID)
        var designTimeTenantContext = new DesignTimeTenantContext();

        return new PlannerDbContext(optionsBuilder.Options, designTimeTenantContext);
    }

    /// <summary>
    /// Dummy tenant context for design-time operations.
    /// Uses a fixed tenant ID that won't affect migration generation.
    /// </summary>
    private class DesignTimeTenantContext : ITenantContext
    {
        public Guid TenantId { get; } = Guid.Parse("00000000-0000-0000-0000-000000000001");

        public bool IsSet => true;

        public void SetTenant(Guid tenantId) { }
    }
}