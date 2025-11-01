using Planner.Messaging;
using Planner.Optimization.Worker;

Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) => {
        var path = Path.Combine(AppContext.BaseDirectory, "shared.appsettings.json");
        config.AddJsonFile(path, optional: false, reloadOnChange: true);
    })
    .ConfigureServices((context, services) => {
        // Register shared Infrastructure (EF, RabbitMQ, etc.)
        services.AddMessagingBus();

        // Register the worker itself
        services.AddHostedService<SolverWorker>();
    })
    .ConfigureLogging(logging => {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .Build()
    .Run();
