using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Planner.API;
using Planner.API.BackgroundServices;
using Planner.Infrastructure.Coordinator;
using Planner.Infrastructure.Persistence;
using Planner.Messaging;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "shared.appsettings.json"), optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

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


// Add services
builder.Services.AddDbContext<PlannerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRazorPages();

builder.Services.AddMessageHub(builder.Configuration);
builder.Services.AddMessagingBus();

// Register your background service
builder.Services.AddHostedService<CoordinatorService>();
builder.Services.AddHostedService<LPResultListener>();
builder.Services.AddHostedService<VRPResultListener>();


var app = builder.Build();

app.UseRouting();
app.UseMessageHub();
app.MapControllers();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthorization();
app.MapRazorPages();
app.MapTaskEndpoints();

app.Run();

