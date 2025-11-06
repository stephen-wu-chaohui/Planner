using Azure.Identity;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Planner.Application.Messaging;
using Planner.BlazorApp.Components;
using Planner.BlazorApp.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "shared.appsettings.json"), optional: false, reloadOnChange: true);

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

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 👇 Add this
builder.Services.AddSingleton(sp => {
    return new HubConnectionBuilder()
        .WithUrl("https://localhost:7085/plannerHub")  // must match API endpoint
        .WithAutomaticReconnect()
        .Build();
});

// 👇 Add this line to enable HttpClient for dependency injection
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IMessageHubClient, OptimizationResultReceiver>();
builder.Services.AddSingleton<DataCenterService>();


var app = builder.Build();

// app.UseMessageHub();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.Use(async (context, next) => {
    context.Response.OnStarting(() => {
        // Remove the built-in header if present
        context.Response.Headers.Remove("Content-Security-Policy");
        return Task.CompletedTask;
    });
    await next();
});


app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
