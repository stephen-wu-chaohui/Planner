using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planner.Application.Features.Configuration;

namespace Planner.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ConfigController(IMediator mediator) : ControllerBase {
    [HttpGet("init")]
    public async Task<IActionResult> GetClientConfiguration(CancellationToken cancellationToken) {
        var tenantData = await mediator.Send(new GetClientConfigurationQuery(), cancellationToken);
        if (tenantData == null) {
            return NotFound("Tenant information not found.");
        }

        return Ok(tenantData);
    }
}
