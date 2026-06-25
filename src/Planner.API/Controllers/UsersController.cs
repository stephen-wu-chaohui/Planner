using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planner.Application.Features.Users;
using System.Diagnostics;

namespace Planner.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsersController(IMediator mediator, ILogger<UsersController> logger) : ControllerBase {
    [HttpGet]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken) {
        var sw = Stopwatch.StartNew();
        logger.LogInformation("Testing DB connectivity for Users table...");

        try {
            var users = await mediator.Send(new GetUsersQuery(), cancellationToken);

            sw.Stop();
            logger.LogInformation("Successfully fetched {Count} users in {Elapsed}ms", users.Count, sw.ElapsedMilliseconds);

            return Ok(new {
                Count = users.Count,
                ElapsedMs = sw.ElapsedMilliseconds,
                Data = users
            });
        } catch (Exception ex) {
            sw.Stop();
            logger.LogError(ex, "Failed to fetch users after {Elapsed}ms", sw.ElapsedMilliseconds);

            return StatusCode(500, new {
                Error = ex.Message,
                InnerError = ex.InnerException?.Message,
                ElapsedMs = sw.ElapsedMilliseconds
            });
        }
    }
}
