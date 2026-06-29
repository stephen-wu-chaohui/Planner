using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planner.Application.CQRS;
using Planner.Application.Features.Optimization;
using Planner.Contracts.Optimization;
using Planner.Contracts.OptimizationRuns;
using Planner.Messaging.Optimization.Inputs;
using Planner.Messaging.Optimization.Outputs;
using System.Security.Cryptography;
using System.Text;

namespace Planner.API.Controllers;

[ApiController]
[Route("api/vrp")]
[Authorize]
public class OptimizationController(
    IMediator mediator,
    IConfiguration configuration) : PlannerControllerBase {
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

    [HttpGet("runs/{optimizationRunId:guid}/insight")]
    public async Task<IActionResult> GetRunInsight(
        Guid optimizationRunId,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(
            new GetOptimizationRunInsightQuery(optimizationRunId),
            cancellationToken);

        return result.Status switch {
            OptimizationResultQueryStatus.Found when result.Insight is not null => Ok(result.Insight),
            OptimizationResultQueryStatus.Accepted => Accepted(result.RunStatus),
            OptimizationResultQueryStatus.NotFound => NotFound(),
            _ => Problem($"Unhandled optimization insight status: {result.Status}")
        };
    }

    [HttpPost("runs/{optimizationRunId:guid}/result")]
    [AllowAnonymous]
    public async Task<IActionResult> CompleteRun(
        Guid optimizationRunId,
        [FromBody] OptimizeRouteResponse response,
        CancellationToken cancellationToken) {
        if (!IsAuthorizedWorkerRequest()) {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (response.OptimizationRunId != optimizationRunId) {
            return BadRequest("Route optimizationRunId does not match the response body.");
        }

        var result = await mediator.Send(
            new CompleteOptimizationRunCommand(response),
            cancellationToken);

        return NoContentOrError(result);
    }

    private Task<OptimizeRouteRequest> BuildRequestFromDomainAsync(int? searchTimeLimitSeconds = null) =>
        mediator.Send(
            new BuildOptimizationRequestQuery(searchTimeLimitSeconds),
            HttpContext?.RequestAborted ?? CancellationToken.None);

    private bool IsAuthorizedWorkerRequest() {
        var expectedApiKey = configuration["Optimization:WorkerResultApiKey"];
        if (string.IsNullOrWhiteSpace(expectedApiKey)) {
            return false;
        }

        if (!Request.Headers.TryGetValue("X-Optimization-Worker-Key", out var providedApiKey)) {
            return false;
        }

        var expectedBytes = Encoding.UTF8.GetBytes(expectedApiKey);
        var providedBytes = Encoding.UTF8.GetBytes(providedApiKey.ToString());

        return expectedBytes.Length == providedBytes.Length
            && CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
