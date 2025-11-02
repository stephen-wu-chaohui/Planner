using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Planner.Application.Messaging;
using Planner.BlazorApp.Components;
using Planner.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "shared.appsettings.json"), optional: false, reloadOnChange: true);

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

builder.Services.AddScoped<IMessageHubClient, OptimizationResultReceiver>();

var app = builder.Build();

// app.UseMessageHub();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
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
