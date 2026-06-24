using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planner.API.Controllers;
using Planner.API.Services;
using Planner.Application;
using Planner.Application.OptimizationRuns;
using Planner.Contracts.OptimizationRuns;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Planner.Optimization;
using Planner.Messaging.Messaging;
using Planner.Infrastructure;
using Planner.Infrastructure.Persistence;
using Planner.Application.Persistence;
using Planner.Messaging.Optimization.Outputs;

namespace Planner.API.EndToEndTests.Fixtures;

public sealed class TestApiFactory : IDisposable {
    public IServiceProvider Services { get; }

    public TestApiFactory(string dispatchMode = "RabbitMq") {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Optimization:DispatchMode"] = dispatchMode
            })
            .Build());

        services.AddLogging();
        services.AddApplication();

        // --- EF Core InMemory ---
        services.AddDbContext<PlannerDbContext>(o =>
            o.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        services.AddScoped<IPlannerDbContext>(sp => sp.GetRequiredService<PlannerDbContext>());

        // --- Tenant context ---
        services.AddScoped<ITenantContext, StaticTenantContext>();

        // --- Message Bus ---
        services.AddScoped<IMessageBus, FakeMessageBus>();

        // --- Optimization ---
        services.AddOptimization();

        // --- Matrix Calculation Service ---
        services.AddScoped<IMatrixCalculationService, MatrixCalculationService>();
        services.AddScoped<IOptimizationRunSnapshotBuilder, OptimizationRunSnapshotBuilder>();
        services.AddScoped<IRouteEnrichmentService, RouteEnrichmentService>();
        services.AddSingleton<IOptimizationRunStore, InMemoryOptimizationRunStore>();
        services.AddSingleton<IOptimizationJobQueue, FakeOptimizationJobQueue>();

        // --- Cache & DataCenter ---
        services.AddDistributedMemoryCache();
        services.AddHybridCache();
        services.AddScoped<IPlannerDataCenter, PlannerDataCenter>();

        // --- Controller ---
        services.AddScoped<OptimizationController>();

        Services = services.BuildServiceProvider();
    }

    public T Get<T>() where T : notnull =>
        Services.GetRequiredService<T>();

    public void Dispose() {
        if (Services is IDisposable d)
            d.Dispose();
    }

    public class StaticTenantContext : ITenantContext {
        public Guid TenantId { get; } = Guid.Parse("00000000-0000-0000-0000-000000000001");
        public string UserEmail => "planner-test@example.com";

        public bool IsSet => true;

        public void SetTenant(Guid tenantId) {
        }
    }

    public sealed class InMemoryOptimizationRunStore : IOptimizationRunStore {
        public Dictionary<Guid, OptimizationRunDocument> Runs { get; } = [];

        public Task<OptimizationRunDocument> CreateAsync(OptimizationRunDocument run, CancellationToken ct) {
            Runs[run.OptimizationRunId] = run;
            return Task.FromResult(run);
        }

        public Task<OptimizationRunDocument?> GetAsync(Guid tenantId, Guid runId, CancellationToken ct) {
            Runs.TryGetValue(runId, out var run);
            return Task.FromResult(run?.TenantId == tenantId ? run : null);
        }

        public Task MarkQueuedAsync(Guid tenantId, Guid runId, CancellationToken ct) {
            if (Runs.TryGetValue(runId, out var run)) {
                Runs[runId] = run with { Status = OptimizationRunStatus.Queued };
            }

            return Task.CompletedTask;
        }

        public Task<bool> TryStartAttemptAsync(Guid tenantId, Guid runId, OptimizationRunAttemptDto attempt, CancellationToken ct) =>
            Task.FromResult(true);

        public Task SaveSolverResultAsync(Guid tenantId, Guid runId, OptimizeRouteResponse result, CancellationToken ct) {
            if (Runs.TryGetValue(runId, out var run)) {
                Runs[runId] = run with { SolverResult = result };
            }

            return Task.CompletedTask;
        }

        public Task SaveFailureAsync(Guid tenantId, Guid runId, string errorMessage, OptimizationRunStatus status, CancellationToken ct) {
            if (Runs.TryGetValue(runId, out var run)) {
                Runs[runId] = run with { ErrorMessage = errorMessage, Status = status };
            }

            return Task.CompletedTask;
        }

        public Task SaveAiInsightAsync(Guid tenantId, Guid runId, OptimizationAiInsightDto insight, CancellationToken ct) {
            if (Runs.TryGetValue(runId, out var run)) {
                Runs[runId] = run with { AiInsight = insight };
            }

            return Task.CompletedTask;
        }
    }

    public sealed class FakeOptimizationJobQueue : IOptimizationJobQueue {
        public List<OptimizationJobMessage> Messages { get; } = [];

        public Task EnqueueAsync(OptimizationJobMessage message, CancellationToken ct) {
            Messages.Add(message);
            return Task.CompletedTask;
        }

        public Task<IOptimizationJobEnvelope?> ReceiveOneAsync(CancellationToken ct) =>
            Task.FromResult<IOptimizationJobEnvelope?>(null);
    }
}
