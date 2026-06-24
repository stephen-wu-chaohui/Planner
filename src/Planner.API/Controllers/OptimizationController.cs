using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planner.Application.CQRS;
using Planner.Application.Features.Optimization;
using Planner.Contracts.Optimization;
using Planner.Contracts.OptimizationRuns;
using Planner.Messaging.Optimization.Inputs;

namespace Planner.API.Controllers;

[ApiController]
[Route("api/vrp")]
[Authorize]
public class OptimizationController(IMediator mediator) : PlannerControllerBase {
    /// <summary>
    /// Accept a route optimization request and dispatch it to the optimization worker.
    /// </summary>
    [HttpGet("solve")]
    public async Task<IActionResult> Solve(
        [FromQuery] int? searchTimeLimitSeconds = null,
        CancellationToken cancellationToken = default) {
        var result = await mediator.Send(
            new SolveOptimizationCommand(searchTimeLimitSeconds),
            cancellationToken);

        return result.Status switch {
            CommandStatus.Succeeded when result.Value is not null => Ok(result.Value),
            CommandStatus.Rejected => BadRequest(result.Error),
            CommandStatus.NotFound => NotFound(),
            _ => Problem($"Unhandled command status: {result.Status}")
        };
    }

    [HttpGet("runs/{optimizationRunId:guid}")]
    public async Task<ActionResult<OptimizationRunStatusDto>> GetRun(
        Guid optimizationRunId,
        CancellationToken cancellationToken) =>
        OkOrNotFound(await mediator.Send(
            new GetOptimizationRunStatusQuery(optimizationRunId),
            cancellationToken));

    [HttpGet("runs/{optimizationRunId:guid}/result")]
    public async Task<IActionResult> GetRunResult(
        Guid optimizationRunId,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(
            new GetOptimizationRunResultQuery(optimizationRunId),
            cancellationToken);

        return result.Status switch {
            OptimizationResultQueryStatus.Found when result.Result is not null => Ok(result.Result),
            OptimizationResultQueryStatus.Accepted => Accepted(result.RunStatus),
            OptimizationResultQueryStatus.NotFound => NotFound(),
            _ => Problem($"Unhandled optimization result status: {result.Status}")
        };
    }

    private Task<OptimizeRouteRequest> BuildRequestFromDomainAsync(int? searchTimeLimitSeconds = null) =>
        mediator.Send(
            new BuildOptimizationRequestQuery(searchTimeLimitSeconds),
            HttpContext?.RequestAborted ?? CancellationToken.None);
}
