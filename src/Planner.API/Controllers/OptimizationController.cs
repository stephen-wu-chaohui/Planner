using Microsoft.AspNetCore.Mvc;
using Planner.Contracts.Optimization.Requests;
using Planner.Contracts.Messaging;
using Planner.Messaging;

namespace Planner.API.Controllers;

[ApiController]
[Route("api/vrp")]
public class OptimizationController(IMessageBus bus) : ControllerBase {
    /// <summary>
    /// Accept a route optimization request and dispatch it to the optimization worker.
    /// </summary>
    [HttpPost("solve")]
    public async Task<IActionResult> Solve([FromBody] OptimizeRouteRequest request) {
        if (request == null || request.Jobs.Count == 0 || request.Vehicles.Count == 0)
            return BadRequest("Invalid request.");

        await bus.PublishAsync(MessageRoutes.OptimizeRoute, request);

        return Accepted(new {
            message = "Optimization request queued for processing.",
            request.OptimizationRunId
        });
    }
}
