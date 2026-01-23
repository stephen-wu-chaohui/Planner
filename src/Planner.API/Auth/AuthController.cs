using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planner.API.Auth;
using Planner.Contracts.API.Auth;
using Planner.Infrastructure.Auth;
using Planner.Infrastructure.Persistence;

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

        // 3. Generate JWT
        var token = tokenGenerator.GenerateToken(
            userId: user.Id,
            tenantId: user.TenantId,
            role: user.Role);

        // 4. Set HTTP-only cookie for BFF authentication
        var cookieOptions = new CookieOptions {
            HttpOnly = true,
            Secure = true, // Requires HTTPS
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7), // 7-day expiration
            Path = "/",
            IsEssential = true
        };
        
        Response.Cookies.Append("planner-auth", token, cookieOptions);

        // 5. Return response (token can be used for initial setup if needed)
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

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout() {
        // Clear the authentication cookie
        Response.Cookies.Delete("planner-auth", new CookieOptions {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });

        return Ok(new { Message = "Logged out successfully" });
    }

    // Demo-only password check
    private static bool VerifyPassword(
        string password,
        string passwordHash) {
        // Replace with real hashing later
        return passwordHash == password;
    }
}
