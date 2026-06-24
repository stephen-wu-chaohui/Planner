using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planner.Application.Features.Tenants;
using Planner.Contracts.API;

namespace Planner.API.Controllers;

[Route("api/tenants")]
[Authorize]
public sealed class TenantsController(IMediator mediator) : PlannerControllerBase {
    /// <summary>
    /// Get tenant metadata including tenant name and main depot.
    /// </summary>
    /// <remarks>
    /// The main depot is determined by selecting the first depot associated with the tenant.
    /// In a future enhancement, this could be replaced with an explicit main depot designation.
    /// </remarks>
    [HttpGet("metadata")]
    public async Task<ActionResult<TenantDto>> GetMetadata(CancellationToken cancellationToken = default) {
        var metadata = await mediator.Send(new GetTenantMetadataQuery(), cancellationToken);
        return metadata is null ? NotFound("Tenant not found.") : Ok(metadata);
    }
}
