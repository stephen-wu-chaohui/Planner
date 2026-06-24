using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planner.API.Services;
using Planner.Application;

namespace Planner.API.Controllers;

[ApiController]
[Authorize]
[Route("api/realtime")]
public sealed class RealtimeController(
    ITenantContext tenantContext,
    IAzureSignalRConnectionInfoService signalRConnectionInfoService) : ControllerBase {

    [HttpGet("negotiate")]
    public IActionResult Negotiate() {
        if (!tenantContext.IsSet || tenantContext.TenantId == Guid.Empty) {
            return Unauthorized();
        }

        return Ok(signalRConnectionInfoService.CreateConnectionInfo(tenantContext.TenantId.ToString()));
    }
}
