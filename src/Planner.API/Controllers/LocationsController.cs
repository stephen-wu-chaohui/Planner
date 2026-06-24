using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planner.Application.Features.Locations;
using Planner.Contracts.API;

namespace Planner.API.Controllers;

[Route("api/locations")]
[Authorize]
public sealed class LocationsController(IMediator mediator) : PlannerControllerBase {
    [HttpGet]
    public async Task<ActionResult<List<LocationDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetLocationsQuery(), cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<LocationDto>> GetById(long id, CancellationToken cancellationToken) =>
        OkOrNotFound(await mediator.Send(new GetLocationByIdQuery(id), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] LocationDto dto,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new CreateLocationCommand(dto), cancellationToken);
        return CreatedOrError(result, created => $"/api/locations/{created.Id}");
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] LocationDto dto,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new UpdateLocationCommand(id, dto), cancellationToken);
        return NoContentOrError(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken) {
        var result = await mediator.Send(new DeleteLocationCommand(id), cancellationToken);
        return NoContentOrError(result);
    }
}
