using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Planner.API.Controllers;

[ApiController]
[Route("auth-test")]
public class AuthTestController : ControllerBase {
    [HttpGet]
    [Authorize]
    public IActionResult Get() {
        return Ok(new {
            User = User.Identity?.Name,
            Claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }
}
