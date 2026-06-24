using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planner.Application.Features.Jobs;
using Planner.Contracts.API;

namespace Planner.API.Controllers;

[Route("api/jobs")]
[Authorize]
public sealed class JobsController(IMediator mediator) : PlannerControllerBase {
    [HttpGet]
    public async Task<ActionResult<List<JobDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetJobsQuery(), cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<JobDto>> GetById(long id, CancellationToken cancellationToken) =>
        OkOrNotFound(await mediator.Send(new GetJobByIdQuery(id), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] JobDto dto,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new CreateJobCommand(dto), cancellationToken);
        return CreatedOrError(result, created => $"/api/jobs/{created.Id}");
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] JobDto dto,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new UpdateJobCommand(id, dto), cancellationToken);
        return NoContentOrError(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken) {
        var result = await mediator.Send(new DeleteJobCommand(id), cancellationToken);
        return NoContentOrError(result);
    }
}
