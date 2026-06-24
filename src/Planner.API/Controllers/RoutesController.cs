using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planner.Application.Features.Routes;
using Route = Planner.Domain.Route;

namespace Planner.API.Controllers;

[Route("api/routes")]
[Authorize]
public sealed class RoutesController(IMediator mediator) : PlannerControllerBase {
    [HttpGet]
    public async Task<ActionResult<List<Route>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetRoutesQuery(), cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<Route>> GetById(long id, CancellationToken cancellationToken) =>
        OkOrNotFound(await mediator.Send(new GetRouteByIdQuery(id), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] Route entity,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new CreateRouteCommand(entity), cancellationToken);
        return CreatedOrError(result, created => $"/api/routes/{created.Id}");
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] Route updated,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new UpdateRouteCommand(id, updated), cancellationToken);
        return NoContentOrError(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken) {
        var result = await mediator.Send(new DeleteRouteCommand(id), cancellationToken);
        return NoContentOrError(result);
    }
}
