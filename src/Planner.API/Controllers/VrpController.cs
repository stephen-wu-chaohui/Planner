using Microsoft.AspNetCore.Mvc;
using Planner.Contracts.Messages;
using Planner.Contracts.Messages.VehicleRoutingProblem;
using Planner.Messaging;

namespace Planner.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VrpController(IMessageBus bus) : ControllerBase {

    /// <summary>
    /// Accept a VRP request and send it to the optimization worker
    /// </summary>
    [HttpPost("solve")]
    public async Task<IActionResult> Solve([FromBody] VrpRequestMessage message) {
        var request = message.Request;
        if (request == null || request.Jobs.Count == 0 || request.Vehicles.Count == 0)
            return BadRequest("Invalid request.");

        await bus.PublishAsync(MessageRoutes.VRPSolverRequest, message);
        Console.WriteLine("📤 VRP request published to queue planner.vrp.requests.");

        // Immediate ack; actual result returned asynchronously via SignalR
        return Accepted(new { message = "VRP request queued for processing." });
    }
}
