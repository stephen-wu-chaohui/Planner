using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Planner.API.Controllers;

[AllowAnonymous] // Must be accessible without a token
[ApiExplorerSettings(IgnoreApi = true)] // Hide from Swagger
public class ErrorsController : ControllerBase {
    [Route("/error")]
    public IActionResult HandleError() {
        var context = HttpContext.Features.Get<IExceptionHandlerFeature>();
        var exception = context?.Error;

        // Log the actual exception details to the console/Azure Log Stream
        Console.WriteLine($"[GLOBAL ERROR] {exception?.Message}");
        if (exception?.InnerException != null) {
            Console.WriteLine($"[INNER ERROR] {exception.InnerException.Message}");
        }

        return Problem(
            detail: exception?.Message,
            title: "An unhandled error occurred in the API Pipeline");
    }
}
