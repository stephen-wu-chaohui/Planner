using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Planner.API;
using Planner.API.BackgroundServices;
using Planner.API.SignalR;
using Planner.Application;
using Planner.Infrastructure.Coordinator;
using Planner.Infrastructure.Persistence;
using Planner.Infrastructure.Seed;
using Planner.Messaging.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
// ✅ Try to load shared.appsettings.json only if it exists
var sharedConfigPath = Path.Combine(AppContext.BaseDirectory, "shared.appsettings.json");
var loggerFactory = LoggerFactory.Create(config => {
    config.AddConsole();
});
var logger = loggerFactory.CreateLogger("Startup");

if (File.Exists(sharedConfigPath)) {
    builder.Configuration.AddJsonFile(sharedConfigPath, optional: true, reloadOnChange: true);
    logger.LogInformation("Loaded shared.appsettings.json from {Path}", sharedConfigPath);
} else {
    logger.LogWarning("shared.appsettings.json not found — continuing with environment & Azure config.");
}

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);

// 🔹 Connect to Azure App Configuration
string? appConfigEndpoint = builder.Configuration["AppConfig:Endpoint"];
if (!string.IsNullOrEmpty(appConfigEndpoint)) {
    builder.Configuration.AddAzureAppConfiguration(options => {
        options.Connect(new Uri(appConfigEndpoint), new DefaultAzureCredential())
               .Select(KeyFilter.Any, LabelFilter.Null)
               .Select(KeyFilter.Any, builder.Environment.EnvironmentName)
               .ConfigureKeyVault(kv => {
                   kv.SetCredential(new DefaultAzureCredential());
               });
    });
}
// 🔹 Load environment variables last to allow overrides
builder.Configuration.AddEnvironmentVariables();

// ---------------------------------------------
// 3️⃣ Add logging & services as usual
// ---------------------------------------------
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Get and log connection string for debugging
var connectionString = builder.Configuration.GetConnectionString("PlannerDb");
logger.LogInformation("Using connection string: {ConnectionString}",
    connectionString != null ? connectionString.Substring(0, Math.Min(50, connectionString.Length)) + "..." : "null");

// Add services
builder.Services.AddDbContext<PlannerDbContext>(options =>
    options.UseSqlServer(
        connectionString ?? throw new InvalidOperationException("Connection string 'PlannerDb' not found.")
    )
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers();
builder.Services.AddRazorPages();

builder.Services.AddRealtime(builder.Configuration);
builder.Services.AddMessagingBus();

// Register your background service
builder.Services.AddHostedService<CoordinatorService>();
builder.Services.AddHostedService<OptimizeRouteResultConsumer>();


builder.Services.AddHealthChecks();

builder.Services.AddScoped<ITenantContext, StaticTenantContext>();

var app = builder.Build();

app.UseRouting();
app.UseRealtime();
app.MapControllers();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthorization();
app.MapRazorPages();
app.MapTaskEndpoints();

// Minimal API endpoints
app.MapGet("/ping", () => "pong");

app.MapGet("/db-check", async (PlannerDbContext db) => {
    var ok = await db.Database.CanConnectAsync();
    return Results.Ok(new { canConnect = ok });
});

app.MapHealthChecks("/health");

app.MapGet("/vehicles", async (
    PlannerDbContext db,
    ITenantContext tenant
) => {
    var vehicles = await db.Vehicles
        // .Where(v => v.TenantId == tenant.TenantId)
        .ToListAsync();

    return Results.Ok(vehicles);
});

app.MapGet("/customers", async (
    PlannerDbContext db,
    ITenantContext tenant
) => {
    var customers = await db.Customers
        .Include(c => c.Location)
        .ToListAsync();

    return Results.Ok(customers);
});

//using (var scope = app.Services.CreateScope()) {
//    var services = scope.ServiceProvider;
//    try {
//        var context = services.GetRequiredService<PlannerDbContext>();
//        context.Database.Migrate();
//        DataSeeder.Seed(context);
//    } catch (Exception ex) {
//        var seederLogger = services.GetRequiredService<ILogger<Program>>();
//        seederLogger.LogError(ex, "An error occurred while seeding the database.");
//    }
//}

app.Run();

