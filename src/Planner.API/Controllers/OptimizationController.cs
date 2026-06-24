using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planner.API.Services;
using Planner.Application;
using Planner.Application.OptimizationRuns;
using Planner.Contracts.Optimization;
using Planner.Contracts.OptimizationRuns;
using Planner.Messaging.Messaging;
using Planner.Messaging.Optimization.Inputs;

namespace Planner.API.Controllers;

[ApiController]
[Route("api/vrp")]
[Authorize]
public class OptimizationController(
    IMessageBus bus,
    IOptimizationRunSnapshotBuilder snapshotBuilder,
    IOptimizationRunStore runStore,
    IOptimizationJobQueue jobQueue,
    ITenantContext tenant,
    IConfiguration configuration,
    IRouteEnrichmentService routeEnrichmentService) : ControllerBase {
    /// <summary>
    /// Accept a route optimization request and dispatch it to the optimization worker.
    /// </summary>
    [HttpGet("solve")]
    public async Task<IActionResult> Solve([FromQuery] int? searchTimeLimitSeconds = null) {
        var ct = HttpContext?.RequestAborted ?? CancellationToken.None;
        var run = await snapshotBuilder.BuildAsync(
            tenant.TenantId,
            tenant.UserEmail,
            searchTimeLimitSeconds,
            ct);

        var request = run.RequestSnapshot;

        if (request.Stops.Length == 0 || request.Vehicles.Length == 0)
            return BadRequest("No jobs or vehicles available for optimization.");

        if (UseAzureServiceBus()) {
            await runStore.CreateAsync(run, ct);
            try {
                await jobQueue.EnqueueAsync(
                    new OptimizationJobMessage(run.TenantId, run.OptimizationRunId),
                    ct);
                await runStore.MarkQueuedAsync(run.TenantId, run.OptimizationRunId, ct);
            } catch (Exception ex) {
                await runStore.SaveFailureAsync(
                    run.TenantId,
                    run.OptimizationRunId,
                    $"Failed to enqueue optimization job: {ex.Message}",
                    OptimizationRunStatus.Failed,
                    ct);
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
            request.Settings?.SearchTimeLimitSeconds ?? 60
        );
        return Ok(summary);
    }

    [HttpGet("runs/{optimizationRunId:guid}")]
    public async Task<IActionResult> GetRun(Guid optimizationRunId, CancellationToken ct) {
        var run = await runStore.GetAsync(tenant.TenantId, optimizationRunId, ct);
        return run is null ? NotFound() : Ok(run.ToStatusDto());
    }

    [HttpGet("runs/{optimizationRunId:guid}/result")]
    public async Task<IActionResult> GetRunResult(Guid optimizationRunId, CancellationToken ct) {
        var run = await runStore.GetAsync(tenant.TenantId, optimizationRunId, ct);
        if (run is null) {
            return NotFound();
        }

        if (run.SolverResult is null) {
            return Accepted(run.ToStatusDto());
        }

        var enriched = await routeEnrichmentService.EnrichAsync(run.SolverResult);
        return Ok(enriched);
    }

    private async Task<OptimizeRouteRequest> BuildRequestFromDomainAsync(int? searchTimeLimitSeconds = null) {
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        var run = await snapshotBuilder.BuildAsync(
            tenant.TenantId,
            tenant.UserEmail,
            searchTimeLimitSeconds,
            cancellationToken);
        return run.RequestSnapshot;
    }

    private bool UseAzureServiceBus() =>
        string.Equals(
            configuration["Optimization:DispatchMode"],
            "AzureServiceBus",
            StringComparison.OrdinalIgnoreCase);
}
