using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Planner.Application.OptimizationRuns;
using Planner.Contracts.OptimizationRuns;
using Planner.Messaging.Optimization.Inputs;
using Planner.Messaging.Optimization.Outputs;
using Planner.Optimization;
using Planner.Optimization.JobWorker;
using Planner.Testing.Fixtures;
using Xunit;

namespace Planner.Optimization.JobWorker.Tests;

public sealed class OptimizationJobProcessorTests {
    [Fact]
    public async Task Invalid_message_body_is_dead_lettered() {
        var envelope = FakeEnvelope.Invalid();
        var processor = CreateProcessor(new FakeQueue(envelope), new FakeStore(), new FakeOptimizer());

        var exitCode = await processor.ProcessOneAsync(CancellationToken.None);

        exitCode.Should().Be(0);
        envelope.DeadLetterReason.Should().Be("InvalidOptimizationJobMessage");
    }

    [Fact]
    public async Task Terminal_run_completes_duplicate_message_without_solving() {
        var run = CreateRun(OptimizationRunStatus.Succeeded);
        var envelope = FakeEnvelope.Valid(run);
        var optimizer = new FakeOptimizer();
        var processor = CreateProcessor(new FakeQueue(envelope), new FakeStore(run), optimizer);

        var exitCode = await processor.ProcessOneAsync(CancellationToken.None);

        exitCode.Should().Be(0);
        envelope.Completed.Should().BeTrue();
        optimizer.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Successful_run_saves_solver_result_and_completes_message() {
        var run = CreateRun(OptimizationRunStatus.Created);
        var envelope = FakeEnvelope.Valid(run);
        var store = new FakeStore(run);
        var optimizer = new FakeOptimizer();
        var processor = CreateProcessor(new FakeQueue(envelope), store, optimizer);

        var exitCode = await processor.ProcessOneAsync(CancellationToken.None);

        exitCode.Should().Be(0);
        envelope.Completed.Should().BeTrue();
        optimizer.CallCount.Should().Be(1);
        store.SavedResult.Should().NotBeNull();
        store.Current.Status.Should().Be(OptimizationRunStatus.Succeeded);
    }

    private static OptimizationJobProcessor CreateProcessor(
        IOptimizationJobQueue queue,
        IOptimizationRunStore store,
        IRouteOptimizer optimizer) =>
        new(
            queue,
            store,
            optimizer,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Worker:Id"] = "worker-test"
            }).Build(),
            NullLogger<OptimizationJobProcessor>.Instance);

    private static OptimizationRunDocument CreateRun(OptimizationRunStatus status) {
        var request = VrpBaseline.CreateSmallDeterministic();
        return new OptimizationRunDocument(
            Id: request.OptimizationRunId.ToString(),
            TenantId: request.TenantId,
            OptimizationRunId: request.OptimizationRunId,
            SchemaVersion: 1,
            Version: 1,
            Status: status,
            RequestedAtUtc: request.RequestedAt,
            UpdatedAtUtc: request.RequestedAt,
            RequestSnapshot: request,
            Summary: new OptimizationRunSummaryDto(
                request.Stops.Length,
                request.Vehicles.Length,
                request.RequestedAt,
                request.Settings?.SearchTimeLimitSeconds ?? 0,
                null),
            SolverResult: null,
            AiInsight: null,
            Timeline: [],
            Attempts: [],
            ErrorMessage: null);
    }

    private sealed class FakeQueue(IOptimizationJobEnvelope envelope) : IOptimizationJobQueue {
        public Task EnqueueAsync(OptimizationJobMessage message, CancellationToken ct) => Task.CompletedTask;

        public Task<IOptimizationJobEnvelope?> ReceiveOneAsync(CancellationToken ct) =>
            Task.FromResult<IOptimizationJobEnvelope?>(envelope);
    }

    private sealed class FakeEnvelope : IOptimizationJobEnvelope {
        private FakeEnvelope(OptimizationJobMessage? message, Exception? deserializationException) {
            Message = message;
            DeserializationException = deserializationException;
        }

        public OptimizationJobMessage? Message { get; }
        public string MessageId => "message-1";
        public string? CorrelationId => Message?.OptimizationRunId.ToString();
        public int DeliveryCount => 1;
        public Exception? DeserializationException { get; }
        public bool Completed { get; private set; }
        public bool Abandoned { get; private set; }
        public string? DeadLetterReason { get; private set; }

        public static FakeEnvelope Invalid() => new(null, new JsonException("invalid"));

        public static FakeEnvelope Valid(OptimizationRunDocument run) =>
            new(new OptimizationJobMessage(run.TenantId, run.OptimizationRunId), null);

        public Task CompleteAsync(CancellationToken ct) {
            Completed = true;
            return Task.CompletedTask;
        }

        public Task AbandonAsync(CancellationToken ct) {
            Abandoned = true;
            return Task.CompletedTask;
        }

        public Task DeadLetterAsync(string reason, string? errorDescription, CancellationToken ct) {
            DeadLetterReason = reason;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeStore(OptimizationRunDocument? initialRun = null) : IOptimizationRunStore {
        public OptimizationRunDocument Current { get; private set; } = initialRun!;
        public OptimizeRouteResponse? SavedResult { get; private set; }

        public Task<OptimizationRunDocument> CreateAsync(OptimizationRunDocument run, CancellationToken ct) =>
            Task.FromResult(run);

        public Task<OptimizationRunDocument?> GetAsync(Guid tenantId, Guid runId, CancellationToken ct) =>
            Task.FromResult(Current?.TenantId == tenantId && Current.OptimizationRunId == runId ? Current : null);

        public Task MarkQueuedAsync(Guid tenantId, Guid runId, CancellationToken ct) => Task.CompletedTask;

        public Task<bool> TryStartAttemptAsync(Guid tenantId, Guid runId, OptimizationRunAttemptDto attempt, CancellationToken ct) {
            Current = Current with {
                Status = OptimizationRunStatus.Running,
                Attempts = [.. Current.Attempts, attempt]
            };
            return Task.FromResult(true);
        }

        public Task SaveSolverResultAsync(Guid tenantId, Guid runId, OptimizeRouteResponse result, CancellationToken ct) {
            SavedResult = result;
            Current = Current with {
                Status = string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? OptimizationRunStatus.Succeeded
                    : OptimizationRunStatus.Failed,
                SolverResult = result
            };
            return Task.CompletedTask;
        }

        public Task SaveFailureAsync(Guid tenantId, Guid runId, string errorMessage, OptimizationRunStatus status, CancellationToken ct) {
            Current = Current with { Status = status, ErrorMessage = errorMessage };
            return Task.CompletedTask;
        }

        public Task SaveAiInsightAsync(Guid tenantId, Guid runId, OptimizationAiInsightDto insight, CancellationToken ct) =>
            Task.CompletedTask;
    }

    private sealed class FakeOptimizer : IRouteOptimizer {
        public int CallCount { get; private set; }

        public OptimizeRouteResponse Optimize(OptimizeRouteRequest request) {
            CallCount++;
            return new OptimizeRouteResponse(request.TenantId, request.OptimizationRunId, DateTime.UtcNow, [], 0);
        }
    }
}
