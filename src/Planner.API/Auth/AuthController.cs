using Microsoft.AspNetCore.Mvc;
using Planner.API.Auth;
using Planner.Infrastructure.Auth;
using Planner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Planner.API.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase {
    private readonly PlannerDbContext _db;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public AuthController(
        PlannerDbContext db,
        IJwtTokenGenerator tokenGenerator) {
        _db = db;
        _tokenGenerator = tokenGenerator;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request) {
        // 1. Load user (demo: simple lookup)
        var user = await _db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email == request.Email);

        if (user is null)
            return Unauthorized("Invalid credentials.");

        // 2. Validate password (demo version)
        if (!VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials.");

        // 3. Generate JWT
        var token = _tokenGenerator.GenerateToken(
            userId: user.Id,
            tenantId: user.TenantId,
            role: user.Role);

        // 4. Return token
        return Ok(new LoginResponse(token));
    }

    // Demo-only password check
    private static bool VerifyPassword(
        string password,
        string passwordHash) {
        // Replace with real hashing later
        return passwordHash == password;
    }
}
