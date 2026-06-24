using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planner.Application.Features.Tasks;
using Planner.Domain;

namespace Planner.API.Controllers;

[Route("api/tasks")]
[Authorize]
public sealed class TasksController(IMediator mediator) : PlannerControllerBase {
    [HttpGet]
    public async Task<ActionResult<List<TaskItem>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetTasksQuery(), cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<TaskItem>> GetById(long id, CancellationToken cancellationToken) =>
        OkOrNotFound(await mediator.Send(new GetTaskByIdQuery(id), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] TaskItem entity,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new CreateTaskCommand(entity), cancellationToken);
        return CreatedOrError(result, created => $"/api/tasks/{created.Id}");
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] TaskItem updated,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new UpdateTaskCommand(id, updated), cancellationToken);
        return NoContentOrError(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken) {
        var result = await mediator.Send(new DeleteTaskCommand(id), cancellationToken);
        return NoContentOrError(result);
    }
}
