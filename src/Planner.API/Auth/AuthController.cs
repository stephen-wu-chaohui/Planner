using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.API.Auth;
using Planner.Contracts.API.Auth;
using Planner.Infrastructure.Persistence.Auth;
using Planner.Infrastructure.Persistence.Persistence;

namespace Planner.API.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController(PlannerDbContext db,IJwtTokenGenerator tokenGenerator) : ControllerBase {
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request) {
        // 1. Load user (demo: simple lookup)
        var user = await db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email == request.Email);

        if (user is null)
            return Unauthorized("Invalid credentials.");

        // 2. Validate password (demo version)
        if (!VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials.");

        // 3. Generate JWT and shape response
        return Ok(user.ToLoginResponse(tokenGenerator));
    }

    [Authorize]
    [HttpGet("diagnose")]
    public IActionResult Diagnose() {
        return Ok(new {
            IsAuthenticated = User.Identity?.IsAuthenticated,
            Claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }

    // Demo-only password check
    private static bool VerifyPassword(
        string password,
        string passwordHash) {
        // Replace with real hashing later
        return passwordHash == password;
    }
}
