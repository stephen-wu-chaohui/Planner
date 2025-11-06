using Azure.Identity;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Planner.Messaging;
using Planner.Optimization.Worker;

Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) => {
        var env = context.HostingEnvironment;

        // Always load local and environment settings first
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
              .AddJsonFile("shared.appsettings.json", optional: true, reloadOnChange: true)
              .AddEnvironmentVariables();

        // Read current configuration to check for Azure App Config endpoint
        var builtConfig = config.Build();
        var appConfigEndpoint = builtConfig["AppConfig:Endpoint"];

        // 🔹 Use Azure App Configuration if available
        if (!string.IsNullOrEmpty(appConfigEndpoint)) {
            config.AddAzureAppConfiguration(options => {
                options.Connect(new Uri(appConfigEndpoint), new DefaultAzureCredential())
                       .Select(KeyFilter.Any, LabelFilter.Null)
                       .Select(KeyFilter.Any, env.EnvironmentName)
                       .ConfigureKeyVault(kv => {
                           kv.SetCredential(new DefaultAzureCredential());
                       });
            });
        }
    })
    .ConfigureServices((context, services) => {
        // Register shared infrastructure (EF, RabbitMQ, etc.)
        services.AddMessagingBus();

        // Register the worker(s)
        services.AddHostedService<SolverWorker>();
        services.AddHostedService<VRPSolverWorker>();
    })
    .ConfigureLogging(logging => {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .Build()
    .Run();
