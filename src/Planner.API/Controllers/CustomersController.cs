using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planner.Application.Features.Customers;
using Planner.Contracts.API;

namespace Planner.API.Controllers;

[Route("api/customers")]
[Authorize]
public sealed class CustomersController(IMediator mediator) : PlannerControllerBase {
    [HttpGet]
    public async Task<ActionResult<List<CustomerDto>>> GetAll(CancellationToken cancellationToken = default) =>
        Ok(await mediator.Send(new GetCustomersQuery(), cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CustomerDto>> GetById(long id, CancellationToken cancellationToken = default) =>
        OkOrNotFound(await mediator.Send(new GetCustomerByIdQuery(id), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CustomerDto dto,
        CancellationToken cancellationToken = default) {
        var result = await mediator.Send(new CreateCustomerCommand(dto), cancellationToken);
        return CreatedOrError(result, created => $"/api/customers/{created.CustomerId}");
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] CustomerDto dto,
        CancellationToken cancellationToken = default) {
        var result = await mediator.Send(new UpdateCustomerCommand(id, dto), cancellationToken);
        return NoContentOrError(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken = default) {
        var result = await mediator.Send(new DeleteCustomerCommand(id), cancellationToken);
        return NoContentOrError(result);
    }
}
