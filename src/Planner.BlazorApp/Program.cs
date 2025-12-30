using Planner.BlazorApp.Components;
using Planner.BlazorApp.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// Try to load shared.appsettings.json only if it exists
var loggerFactory = LoggerFactory.Create(config => {
    config.AddConsole();
});
var logger = loggerFactory.CreateLogger("Startup");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options => options.DetailedErrors = true);

builder.Services.AddServerSideBlazor().AddCircuitOptions(o => o.DetailedErrors = true);

// Add SignalR client
builder.Services.AddScoped<IOptimizationHubClient, OptimizationHubClient>();

// Add this line to enable HttpClient for dependency injection
builder.Services.AddHttpClient("PlannerApi", client => {
    client.BaseAddress = new Uri(
        builder.Configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException("Api:BaseUrl not configured")
    );
});

// Provide a default HttpClient instance backed by the named client.
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("PlannerApi"));

builder.Services.AddScoped<DataCenterState>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
// 2. Routing second
app.UseRouting();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
