using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planner.Application.Features.Vehicles;
using Planner.Contracts.API;

namespace Planner.API.Controllers;

[Route("api/vehicles")]
[Authorize]
public sealed class VehiclesController(IMediator mediator) : PlannerControllerBase {
    [HttpGet]
    public async Task<ActionResult<List<VehicleDto>>> GetAll(CancellationToken cancellationToken) {
        var result = await mediator.Send(new GetVehiclesQuery(), cancellationToken);

        if (result.OmittedCount > 0) {
            Response.Headers.Append(
                "X-Warning",
                $"{result.OmittedCount} vehicle(s) omitted due to missing StartDepot/EndDepot navigation.");
        }

        return Ok(result.Items);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<VehicleDto>> GetById(long id, CancellationToken cancellationToken) =>
        OkOrNotFound(await mediator.Send(new GetVehicleByIdQuery(id), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] VehicleDto dto,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new CreateVehicleCommand(dto), cancellationToken);
        return CreatedOrError(result, created => $"/api/vehicles/{created.Id}");
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] VehicleDto dto,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new UpdateVehicleCommand(id, dto), cancellationToken);
        return NoContentOrError(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken) {
        var result = await mediator.Send(new DeleteVehicleCommand(id), cancellationToken);
        return NoContentOrError(result);
    }
}
