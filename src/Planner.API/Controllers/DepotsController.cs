using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planner.Application.Features.Depots;
using Planner.Contracts.API;

namespace Planner.API.Controllers;

[Route("api/depots")]
[Authorize]
public sealed class DepotsController(IMediator mediator) : PlannerControllerBase {
    [HttpGet]
    public async Task<ActionResult<List<DepotDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetDepotsQuery(), cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<DepotDto>> GetById(long id, CancellationToken cancellationToken) =>
        OkOrNotFound(await mediator.Send(new GetDepotByIdQuery(id), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] DepotDto dto,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new CreateDepotCommand(dto), cancellationToken);
        return CreatedOrError(result, created => $"/api/depots/{created.Id}");
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] DepotDto dto,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new UpdateDepotCommand(id, dto), cancellationToken);
        return NoContentOrError(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken) {
        var result = await mediator.Send(new DeleteDepotCommand(id), cancellationToken);
        return NoContentOrError(result);
    }
}
