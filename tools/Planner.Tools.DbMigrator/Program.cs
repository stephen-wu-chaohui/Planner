using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Planner.Application;
using Planner.Infrastructure;
using Planner.Infrastructure.Persistence;
using Planner.Tools.DbMigrator;
using Planner.Tools.DbMigrator.Db;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// ------------------------------------------------------
// Configuration
// ------------------------------------------------------

builder.Configuration
    .AddEnvironmentVariables();

// ------------------------------------------------------
// Services
// ------------------------------------------------------

// tooling tenant (non-HTTP host)
builder.Services.AddScoped<ITenantContext, DbMigratorTenantContext>();

// infrastructure (DbContext, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// seed infrastructure
builder.Services.AddSingleton<SeedHistoryRepository>();

// load SQL scripts once
builder.Services.AddSingleton<IReadOnlyList<SqlScript>>(SqlScriptLoader.Load());

// ------------------------------------------------------
// Build host
// ------------------------------------------------------

using var host = builder.Build();

// ------------------------------------------------------
// Command dispatch
// ------------------------------------------------------

if (args.Length == 0) {
    throw new InvalidOperationException(
        "Expected command: migrate | seed");
}

using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;
var configuration = services.GetRequiredService<IConfiguration>();

var connectionString =
    configuration.GetConnectionString("PlannerDb")
    ?? throw new InvalidOperationException(
        "Connection string 'PlannerDb' not found.");

switch (args[0].ToLowerInvariant()) {
    case "migrate":
        await RunMigrateAsync(scope);
        break;

    case "seed":
        await RunSeedAsync(scope, connectionString);
        break;

    default:
        throw new InvalidOperationException(
            $"Unknown command '{args[0]}'. Expected migrate | seed.");
}


// ------------------------------------------------------
// Local functions
// ------------------------------------------------------

static async Task RunMigrateAsync(IServiceScope scope) {
    // Retrieve the DbContext from the DI container
    // Note: Use your actual DbContext class name (e.g., PlannerDbContext)
    var db = scope.ServiceProvider.GetRequiredService<PlannerDbContext>();

    Console.WriteLine(">>> Starting EF Core Migrations...");

    // Get list of migrations not yet applied to the target database
    var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
    var migrationList = pendingMigrations.ToList();

    if (migrationList.Count == 0) {
        Console.WriteLine(">>> No pending migrations found. Database is up to date.");
        return;
    }

    Console.WriteLine($">>> Found {migrationList.Count} pending migration(s):");
    foreach (var migration in migrationList) {
        Console.WriteLine($"    - {migration}");
    }

    // Apply migrations to the database
    await db.Database.MigrateAsync();

    Console.WriteLine(">>> Migrations applied successfully.");
}


static async Task RunSeedAsync(
    IServiceScope scope,
    string connectionString) {
    var scripts =
        scope.ServiceProvider.GetRequiredService<IReadOnlyList<SqlScript>>();

    var seedHistoryRepository =
        scope.ServiceProvider.GetRequiredService<SeedHistoryRepository>();

    var runner = new SqlSeedRunner(
        connectionString,
        scripts,
        seedHistoryRepository);

    await runner.RunAsync();
}
