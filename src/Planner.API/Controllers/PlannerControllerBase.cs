using Microsoft.AspNetCore.Mvc;

namespace Planner.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class PlannerControllerBase : ControllerBase {
    protected IActionResult ProblemFromException(Exception ex)
        => Problem(
            title: ex.GetType().Name,
            detail: ex.Message,
            statusCode: StatusCodes.Status400BadRequest);
}
