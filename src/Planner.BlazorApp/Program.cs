using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Planner.BlazorApp.Auth;
using Planner.BlazorApp.Components;
using Planner.BlazorApp.Components.WelcomeWizard;
using Planner.BlazorApp.Services;
using Planner.BlazorApp.State;
using Planner.BlazorApp.State.Interfaces;
using Planner.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// Add Authentication
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options => {
        builder.Configuration.GetSection("AzureAd").Bind(options);
        // Map the 'name' claim from Azure AD to the Identity.Name property
        options.TokenValidationParameters.NameClaimType = "name";
    })
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

// Add this to enable the Login/Logout controller views
builder.Services.AddRazorPages().AddMicrosoftIdentityUI();

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

builder.Services.AddControllersWithViews();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options => options.DetailedErrors = true);

builder.Services.AddServerSideBlazor()
    .AddHubOptions(options => {
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    });

// API client (keep named client for BaseAddress with auth handler)
builder.Services.AddTransient<AuthorizationMessageHandler>();
builder.Services.AddHttpClient("PlannerApi", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException("Api:BaseUrl not configured")
    );
}).AddHttpMessageHandler<AuthorizationMessageHandler>();

builder.Services.AddScoped<PlannerApiClient>();

// Shared infrastructure
builder.Services.AddMessagingBus();

// App services
builder.Services.AddScoped<IOptimizationResultsListenerService, OptimizationResultsListenerService>();
builder.Services.AddScoped<IRouteInsightsListenerService, RouteInsightsListenerService>();

builder.Services.AddScoped<DispatchCenterState>();
builder.Services.AddScoped<ITenantState>(sp => sp.GetRequiredService<DispatchCenterState>());
builder.Services.AddScoped<IVehicleState>(sp => sp.GetRequiredService<DispatchCenterState>());
builder.Services.AddScoped<ICustomerState>(sp => sp.GetRequiredService<DispatchCenterState>());
builder.Services.AddScoped<IJobState>(sp => sp.GetRequiredService<DispatchCenterState>());
builder.Services.AddScoped<IRouteState>(sp => sp.GetRequiredService<DispatchCenterState>());
builder.Services.AddScoped<IInsightState>(sp => sp.GetRequiredService<DispatchCenterState>());

builder.Services.AddScoped<WizardService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllers(); // Required for the Login/Logout routes
app.MapGet("/", () => Results.Redirect("/dispatch-center"));

app.MapGet("/demo-login", (string hint, HttpContext context) => {
    var properties = new AuthenticationProperties { RedirectUri = "/" };

    // This is the magic: it tells Entra ID which user is trying to log in
    properties.Items["login_hint"] = hint;

    // Triggers the Microsoft challenge with the hint attached
    return Results.Challenge(properties, [OpenIdConnectDefaults.AuthenticationScheme]);
});
app.Run();
