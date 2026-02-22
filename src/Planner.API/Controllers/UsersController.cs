using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.Infrastructure.Persistence;
using System.Diagnostics;

namespace Planner.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IPlannerDbContext context, ILogger<UsersController> logger) : ControllerBase {
    [HttpGet]
    public async Task<IActionResult> GetUsers() {
        var sw = Stopwatch.StartNew();
        logger.LogInformation("Testing DB connectivity for Users table...");

        try {
            // This specifically tests the new 'Role' column and the DB connection
            var users = await context.Users
                .Select(u => new { u.Email, u.Role, u.CreatedAt })
                .ToListAsync();

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

            // Returns detailed error to help diagnose connection/schema issues
            return StatusCode(500, new {
                Error = ex.Message,
                InnerError = ex.InnerException?.Message,
                ElapsedMs = sw.ElapsedMilliseconds
            });
        }
    }
}