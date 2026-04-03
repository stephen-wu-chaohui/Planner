using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Planner.BlazorApp.Auth;
using Planner.BlazorApp.Components;
using Planner.BlazorApp.Components.WelcomeWizard;
using Planner.BlazorApp.Services;
using Planner.BlazorApp.State;
using Planner.BlazorApp.State.Interfaces;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Azure AD authentication via MSAL
builder.Services.AddMsalAuthentication(options => {
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);

    var scope = builder.Configuration["Api:Scope"];
    if (!string.IsNullOrWhiteSpace(scope))
        options.ProviderOptions.DefaultAccessTokenScopes.Add(scope);

    options.ProviderOptions.LoginMode = "redirect";
});

// API client with MSAL bearer token handler
builder.Services.AddTransient<AuthorizationMessageHandler>();
builder.Services.AddHttpClient("PlannerApi", client => {
    client.BaseAddress = new Uri(
        builder.Configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException("Api:BaseUrl not configured")
    );
}).AddHttpMessageHandler<AuthorizationMessageHandler>();

builder.Services.AddScoped<PlannerApiClient>();

// App services – WASM-compatible implementations replace server-side Firestore listeners
builder.Services.AddScoped<IOptimizationResultsListenerService, PollingOptimizationResultsListenerService>();
builder.Services.AddScoped<IRouteInsightsListenerService, NoOpRouteInsightsListenerService>();

builder.Services.AddScoped<DispatchCenterState>();
builder.Services.AddScoped<ITenantState>(sp => sp.GetRequiredService<DispatchCenterState>());
builder.Services.AddScoped<IVehicleState>(sp => sp.GetRequiredService<DispatchCenterState>());
builder.Services.AddScoped<ICustomerState>(sp => sp.GetRequiredService<DispatchCenterState>());
builder.Services.AddScoped<IJobState>(sp => sp.GetRequiredService<DispatchCenterState>());
builder.Services.AddScoped<IRouteState>(sp => sp.GetRequiredService<DispatchCenterState>());
builder.Services.AddScoped<IInsightState>(sp => sp.GetRequiredService<DispatchCenterState>());

builder.Services.AddScoped<WizardService>();

await builder.Build().RunAsync();
