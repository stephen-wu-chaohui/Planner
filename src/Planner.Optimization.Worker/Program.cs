using Azure.Identity;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Planner.Messaging;
using Planner.Optimization.Worker;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

var sharedConfigPath = Path.Combine(AppContext.BaseDirectory, "shared.appsettings.json");
if (File.Exists(sharedConfigPath)) {
    builder.Configuration.AddJsonFile(sharedConfigPath, optional: true, reloadOnChange: true);
    Console.WriteLine($"Loaded shared.appsettings.json from {sharedConfigPath}");
} else {
    Console.WriteLine("shared.appsettings.json not found — continuing with environment & Azure config.");
}

var appConfigEndpoint = builder.Configuration["AppConfig:Endpoint"];

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

builder.Configuration.AddEnvironmentVariables();

// =========================================================
// SERVICES
// =========================================================

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Shared infrastructure
builder.Services.AddMessagingBus();

// Your worker services
builder.Services.AddHostedService<SolverWorker>();
builder.Services.AddHostedService<VRPSolverWorker>();

// =========================================================
// BUILD APP (bind Kestrel) — required on Windows App Service
// =========================================================

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
