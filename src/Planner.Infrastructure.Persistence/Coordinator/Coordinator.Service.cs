using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Planner.Domain;
using System.Xml.Serialization;

namespace Planner.Infrastructure.Persistence.Coordinator;

public class CoordinatorService(ILogger<CoordinatorService> logger, IServiceProvider services) : BackgroundService {
    private readonly ILogger<CoordinatorService> _logger = logger;
    private readonly IServiceProvider _services = services;
    private readonly string _xmlPath = Path.Combine(AppContext.BaseDirectory, "coordinator.xml");
    private CoordinatorConfig? _config;
    private FileSystemWatcher? _watcher;

    protected override async Task ExecuteAsync(CancellationToken token) {
        LoadConfig();
        WatchXml();
        _logger.LogInformation("Coordinator running...");
        while (!token.IsCancellationRequested) {
            if (_config == null) { await Task.Delay(60000, token); continue; }
            foreach (var group in _config.TaskGroups)
                foreach (var task in group.Tasks) {
                    bool run = false;
                    var now = DateTime.UtcNow;
                    if (task.RunTime.HasValue) {
                        var target = DateTime.UtcNow.Date + task.RunTime.Value;
                        if (Math.Abs((now - target).TotalMinutes) < 1) run = true;
                    }
                    if (task.IntervalMinutes.HasValue &&
                        (!task.LastRunUtc.HasValue || (now - task.LastRunUtc.Value).TotalMinutes >= task.IntervalMinutes.Value))
                        run = true;
                    if (run) {
                        await ExecuteTaskAsync(task);
                        task.LastRunUtc = DateTime.UtcNow;
                    }
                }
            await Task.Delay(60000, token);
        }
    }

    private void LoadConfig() {
        if (!File.Exists(_xmlPath)) return;
        var ser = new XmlSerializer(typeof(CoordinatorConfig));
        using var r = File.OpenText(_xmlPath);
        _config = (CoordinatorConfig?)ser.Deserialize(r);
    }

    private void WatchXml() {
        var dir = Path.GetDirectoryName(_xmlPath);
        if (dir == null) return;
        _watcher = new FileSystemWatcher(dir, Path.GetFileName(_xmlPath));
        _watcher.Changed += (_, _) => LoadConfig();
        _watcher.EnableRaisingEvents = true;
    }

    private async Task ExecuteTaskAsync(CoordinatorTask task) {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlannerDbContext>();
        try {
            await db.Database.ExecuteSqlAsync($"EXEC {task.ProcedureName}");
            db.SystemEvents.Add(new SystemEvent { Source = "Coordinator", Message = $"Executed {task.ProcedureName}", Timestamp = DateTime.UtcNow });
        } catch (Exception ex) {
            db.SystemEvents.Add(new SystemEvent { Source = "Coordinator", Message = $"Error {task.ProcedureName}: {ex.Message}", IsError = true, Timestamp = DateTime.UtcNow });
        }
        await db.SaveChangesAsync();
    }
}
