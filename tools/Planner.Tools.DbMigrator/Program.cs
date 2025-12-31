using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Planner.Infrastructure;
using Planner.Infrastructure.Persistence;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<SqlScriptLoader>();
builder.Services.AddSingleton<SeedHistoryRepository>();
builder.Services.AddSingleton<SqlSeedRunner>();

var host = builder.Build();
using var scope = host.Services.CreateScope();

var cmd = args.FirstOrDefault()?.ToLowerInvariant();

return cmd switch {
    "migrate" => await Migrate(scope),
    "seed" => await Seed(scope),
    _ => Usage()
};

static async Task<int> Migrate(IServiceScope scope) {
    var db = scope.ServiceProvider.GetRequiredService<PlannerDbContext>();
    Console.WriteLine("Applying EF migrations...");
    await db.Database.MigrateAsync();
    return 0;
}

static async Task<int> Seed(IServiceScope scope) {
    var runner = scope.ServiceProvider.GetRequiredService<SqlSeedRunner>();
    await runner.RunAsync();
    return 0;
}

static int Usage() {
    Console.Error.WriteLine("Usage: migrate | seed");
    return 1;
}
