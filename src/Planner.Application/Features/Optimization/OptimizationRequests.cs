using MediatR;
using Microsoft.Extensions.Configuration;
using Planner.Application.CQRS;
using Planner.Application;
using Planner.Application.OptimizationRuns;
using Planner.Contracts.Optimization;
using Planner.Contracts.OptimizationRuns;
using Planner.Messaging.Messaging;
using Planner.Messaging.Optimization.Inputs;

namespace Planner.Application.Features.Optimization;

public enum OptimizationResultQueryStatus {
    Found,
    NotFound,
    Accepted
}

public sealed record OptimizationResultQueryResult(
    OptimizationResultQueryStatus Status,
    OptimizationRunStatusDto? RunStatus = null,
    RoutingResultDto? Result = null);

public sealed record SolveOptimizationCommand(int? SearchTimeLimitSeconds)
    : IRequest<CommandResult<OptimizationSummary>>;

public sealed record GetOptimizationRunStatusQuery(Guid OptimizationRunId)
    : IRequest<OptimizationRunStatusDto?>;

public sealed record GetOptimizationRunResultQuery(Guid OptimizationRunId)
    : IRequest<OptimizationResultQueryResult>;

public sealed record BuildOptimizationRequestQuery(int? SearchTimeLimitSeconds)
    : IRequest<OptimizeRouteRequest>;

public sealed class OptimizationCommandHandler(
    IMessageBus bus,
    IOptimizationRunSnapshotBuilder snapshotBuilder,
    IOptimizationRunStore runStore,
    IOptimizationJobQueue jobQueue,
    ITenantContext tenant,
    IConfiguration configuration) :
    IRequestHandler<SolveOptimizationCommand, CommandResult<OptimizationSummary>> {
    public async Task<CommandResult<OptimizationSummary>> Handle(
        SolveOptimizationCommand command,
        CancellationToken cancellationToken) {
        var run = await snapshotBuilder.BuildAsync(
            tenant.TenantId,
            tenant.UserEmail,
            command.SearchTimeLimitSeconds,
            cancellationToken);

        var request = run.RequestSnapshot;

        if (request.Stops.Length == 0 || request.Vehicles.Length == 0) {
            return CommandResult<OptimizationSummary>.Rejected("No jobs or vehicles available for optimization.");
        }

        if (UseAzureServiceBus()) {
            await runStore.CreateAsync(run, cancellationToken);
            try {
                await jobQueue.EnqueueAsync(
                    new OptimizationJobMessage(run.TenantId, run.OptimizationRunId),
                    cancellationToken);
                await runStore.MarkQueuedAsync(run.TenantId, run.OptimizationRunId, cancellationToken);
            } catch (Exception ex) {
                await runStore.SaveFailureAsync(
                    run.TenantId,
                    run.OptimizationRunId,
                    $"Failed to enqueue optimization job: {ex.Message}",
                    OptimizationRunStatus.Failed,
                    cancellationToken);
                throw;
            }
        } else {
            await bus.PublishAsync(MessageRoutes.Request, request);
        }

        var summary = new OptimizationSummary(
            request.TenantId,
            request.OptimizationRunId,
            request.Stops.Length,
            request.Vehicles.Length,
            request.RequestedAt,
            request.Settings?.SearchTimeLimitSeconds ?? 60);

        return CommandResult<OptimizationSummary>.Succeeded(summary);
    }

    private bool UseAzureServiceBus() =>
        string.Equals(
            configuration["Optimization:DispatchMode"],
            "AzureServiceBus",
            StringComparison.OrdinalIgnoreCase);
}

public sealed class OptimizationQueryHandler(
    IOptimizationRunStore runStore,
    ITenantContext tenant,
    IOptimizationRunSnapshotBuilder snapshotBuilder,
    IRouteEnrichmentService routeEnrichmentService) :
    IRequestHandler<GetOptimizationRunStatusQuery, OptimizationRunStatusDto?>,
    IRequestHandler<GetOptimizationRunResultQuery, OptimizationResultQueryResult>,
    IRequestHandler<BuildOptimizationRequestQuery, OptimizeRouteRequest> {
    public async Task<OptimizationRunStatusDto?> Handle(
        GetOptimizationRunStatusQuery query,
        CancellationToken cancellationToken) {
        var run = await runStore.GetAsync(tenant.TenantId, query.OptimizationRunId, cancellationToken);
        return run?.ToStatusDto();
    }

    public async Task<OptimizationResultQueryResult> Handle(
        GetOptimizationRunResultQuery query,
        CancellationToken cancellationToken) {
        var run = await runStore.GetAsync(tenant.TenantId, query.OptimizationRunId, cancellationToken);
        if (run is null) {
            return new OptimizationResultQueryResult(OptimizationResultQueryStatus.NotFound);
        }

        if (run.SolverResult is null) {
            return new OptimizationResultQueryResult(
                OptimizationResultQueryStatus.Accepted,
                RunStatus: run.ToStatusDto());
        }

        var enriched = await routeEnrichmentService.EnrichAsync(run.SolverResult);
        return new OptimizationResultQueryResult(
            OptimizationResultQueryStatus.Found,
            Result: enriched);
    }

    public async Task<OptimizeRouteRequest> Handle(
        BuildOptimizationRequestQuery query,
        CancellationToken cancellationToken) {
        var run = await snapshotBuilder.BuildAsync(
            tenant.TenantId,
            tenant.UserEmail,
            query.SearchTimeLimitSeconds,
            cancellationToken);

        return run.RequestSnapshot;
    }
}
