using Microsoft.AspNetCore.Mvc;
using Planner.Application.CQRS;

namespace Planner.API.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class PlannerControllerBase : ControllerBase {
    protected IActionResult ProblemFromException(Exception ex)
        => Problem(
            title: ex.GetType().Name,
            detail: ex.Message,
            statusCode: StatusCodes.Status400BadRequest);

    protected ActionResult<T> OkOrNotFound<T>(T? value)
        where T : class =>
        value is null ? NotFound() : Ok(value);

    protected IActionResult NoContentOrError(CommandResult result) =>
        result.Status switch {
            CommandStatus.Succeeded => NoContent(),
            CommandStatus.NotFound => NotFound(),
            CommandStatus.Rejected => BadRequest(result.Error),
            _ => Problem($"Unhandled command status: {result.Status}")
        };

    protected IActionResult NoContentOrError<T>(CommandResult<T> result) =>
        result.Status switch {
            CommandStatus.Succeeded => NoContent(),
            CommandStatus.NotFound => NotFound(),
            CommandStatus.Rejected => BadRequest(result.Error),
            _ => Problem($"Unhandled command status: {result.Status}")
        };

    protected IActionResult CreatedOrError<T>(
        CommandResult<T> result,
        Func<T, string> locationFactory)
        where T : class =>
        result.Status switch {
            CommandStatus.Succeeded when result.Value is not null => Created(locationFactory(result.Value), result.Value),
            CommandStatus.NotFound => NotFound(),
            CommandStatus.Rejected => BadRequest(result.Error),
            _ => Problem($"Unhandled command status: {result.Status}")
        };
}
