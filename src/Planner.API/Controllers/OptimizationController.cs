using Microsoft.AspNetCore.Mvc;
using Planner.Contracts.Messages;
using Planner.Contracts.Messages.LinearSolver;
using Planner.Messaging;

namespace Planner.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OptimizationController(IMessageBus bus) : ControllerBase {
    [HttpPost("linearsolve")]
    public async Task<IActionResult> LinearSolveAsync([FromBody] LinearSolverRequestMessage request) {
        if (request == null)
            return BadRequest("Missing or invalid request payload.");

        await bus.PublishAsync(MessageRoutes.LPSolverRequest, request);
        return Accepted(new { status = "queued", projectId = request.RequestId });
    }
}
