using Planner.BlazorApp.Auth;
using Planner.BlazorApp.Components;
using Planner.BlazorApp.Services;
using Planner.BlazorApp.State;
using Planner.BlazorApp.State.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options => options.DetailedErrors = true);

// JWT in-memory store
builder.Services.AddScoped<IJwtTokenStore, JwtTokenStore>();

// API client (keep named client for BaseAddress; no auth handler)
builder.Services.AddHttpClient("PlannerApi", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException("Api:BaseUrl not configured")
    );
});

builder.Services.AddServerSideBlazor()
    .AddHubOptions(options => {
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    });

builder.Services.AddScoped<PlannerApiClient>();

// App services
builder.Services.AddScoped<IOptimizationHubClient, OptimizationHubClient>();

builder.Services.AddScoped<DispatchCenterState>();
builder.Services.AddScoped<ITenantState>(sp => sp.GetRequiredService<DispatchCenterState>());
builder.Services.AddScoped<IVehicleState>(sp => sp.GetRequiredService<DispatchCenterState>());
builder.Services.AddScoped<ICustomerState>(sp => sp.GetRequiredService<DispatchCenterState>());
builder.Services.AddScoped<IJobState>(sp => sp.GetRequiredService<DispatchCenterState>());
builder.Services.AddScoped<IRouteState>(sp => sp.GetRequiredService<DispatchCenterState>());

builder.Services.AddScoped<WizardService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

app.MapGet("/", () => Results.Redirect("/login"));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
