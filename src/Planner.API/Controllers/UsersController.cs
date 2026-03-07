using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.API.Caching;
using Planner.Infrastructure;
using System.Diagnostics;

namespace Planner.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IPlannerDataCenter dataCenter, ILogger<UsersController> logger) : ControllerBase {
    [HttpGet]
    public async Task<IActionResult> GetUsers() {
        var sw = Stopwatch.StartNew();
        logger.LogInformation("Testing DB connectivity for Users table...");

        try {
            var users = await dataCenter.GetOrFetchAsync(
                CacheKeys.UsersList(),
                async () => await dataCenter.DbContext.Users
                    .Select(u => new UserSummary(u.Email, u.Role, u.CreatedAt))
                    .ToListAsync());

            var result = users ?? [];

            sw.Stop();
            logger.LogInformation("Successfully fetched {Count} users in {Elapsed}ms", result.Count, sw.ElapsedMilliseconds);

            return Ok(new {
                Count = result.Count,
                ElapsedMs = sw.ElapsedMilliseconds,
                Data = result
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

    private sealed record UserSummary(string Email, string Role, DateTime CreatedAt);
}
