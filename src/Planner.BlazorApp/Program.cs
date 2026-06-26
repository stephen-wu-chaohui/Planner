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

var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Api:BaseUrl not configured");

builder.Services.AddTransient<PlannerApiAuthorizationMessageHandler>();
builder.Services.AddHttpClient("PlannerApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<PlannerApiAuthorizationMessageHandler>();

builder.Services.AddScoped<PlannerApiClient>();

builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);

    var apiScope = builder.Configuration["Api:Scope"];
    if (!string.IsNullOrWhiteSpace(apiScope))
    {
        options.ProviderOptions.DefaultAccessTokenScopes.Add(apiScope);
    }
});

builder.Services.AddScoped<IOptimizationResultsListenerService, SignalROptimizationResultsListenerService>();

builder.Services.AddScoped<DispatchCenterState>();
builder.Services.AddScoped<ITenantState>(sp => sp.GetRequiredService<DispatchCenterState>());
builder.Services.AddScoped<IVehicleState>(sp => sp.GetRequiredService<DispatchCenterState>());
builder.Services.AddScoped<ICustomerState>(sp => sp.GetRequiredService<DispatchCenterState>());
builder.Services.AddScoped<IJobState>(sp => sp.GetRequiredService<DispatchCenterState>());
builder.Services.AddScoped<IRouteState>(sp => sp.GetRequiredService<DispatchCenterState>());
builder.Services.AddScoped<IInsightState>(sp => sp.GetRequiredService<DispatchCenterState>());

builder.Services.AddScoped<WizardService>();

await builder.Build().RunAsync();
