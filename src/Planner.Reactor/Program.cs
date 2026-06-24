using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Planner.Reactor;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services => {
        services.AddSingleton<IOptimizationRunNotifier, AzureSignalROptimizationRunNotifier>();
    })
    .Build();

await host.RunAsync();
