using MediatR;
using Planner.Application.CQRS;
using Planner.Application.Features.Customers;
using Planner.Application.Features.Depots;
using Planner.Application.Features.Jobs;
using Planner.Application.Features.Locations;
using Planner.Application.Features.Tasks;
using Planner.Application.Features.Vehicles;
using Planner.Contracts.API;
using Planner.Domain;

namespace Planner.API.GraphQL;

public sealed class Mutation {
    public async Task<JobDto> CreateJob(
        JobDto input,
        [Service] IMediator mediator,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new CreateJobCommand(input), cancellationToken);
        return ValueOrThrow(result);
    }

    public async Task<JobDto?> UpdateJob(
        long id,
        JobDto input,
        [Service] IMediator mediator,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new UpdateJobCommand(id, input), cancellationToken);
        return NullableValueOrThrow(result);
    }

    public async Task<bool> DeleteJob(
        long id,
        [Service] IMediator mediator,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new DeleteJobCommand(id), cancellationToken);
        return DeletedOrThrow(result);
    }

    public async Task<CustomerDto> CreateCustomer(
        CustomerDto input,
        [Service] IMediator mediator,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new CreateCustomerCommand(input), cancellationToken);
        return ValueOrThrow(result);
    }

    public async Task<CustomerDto?> UpdateCustomer(
        long id,
        CustomerDto input,
        [Service] IMediator mediator,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new UpdateCustomerCommand(id, input), cancellationToken);
        return NullableValueOrThrow(result);
    }

    public async Task<bool> DeleteCustomer(
        long id,
        [Service] IMediator mediator,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new DeleteCustomerCommand(id), cancellationToken);
        return DeletedOrThrow(result);
    }

    public async Task<VehicleDto> CreateVehicle(
        VehicleDto input,
        [Service] IMediator mediator,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new CreateVehicleCommand(input), cancellationToken);
        return ValueOrThrow(result);
    }

    public async Task<VehicleDto?> UpdateVehicle(
        long id,
        VehicleDto input,
        [Service] IMediator mediator,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new UpdateVehicleCommand(id, input), cancellationToken);
        return NullableValueOrThrow(result);
    }

    public async Task<bool> DeleteVehicle(
        long id,
        [Service] IMediator mediator,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new DeleteVehicleCommand(id), cancellationToken);
        return DeletedOrThrow(result);
    }

    public async Task<DepotDto> CreateDepot(
        DepotDto input,
        [Service] IMediator mediator,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new CreateDepotCommand(input), cancellationToken);
        return ValueOrThrow(result);
    }

    public async Task<DepotDto?> UpdateDepot(
        long id,
        DepotDto input,
        [Service] IMediator mediator,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new UpdateDepotCommand(id, input), cancellationToken);
        return NullableValueOrThrow(result);
    }

    public async Task<bool> DeleteDepot(
        long id,
        [Service] IMediator mediator,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new DeleteDepotCommand(id), cancellationToken);
        return DeletedOrThrow(result);
    }

    public async Task<LocationDto> CreateLocation(
        LocationDto input,
        [Service] IMediator mediator,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new CreateLocationCommand(input), cancellationToken);
        return ValueOrThrow(result);
    }

    public async Task<LocationDto?> UpdateLocation(
        long id,
        LocationDto input,
        [Service] IMediator mediator,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new UpdateLocationCommand(id, input), cancellationToken);
        return NullableValueOrThrow(result);
    }

    public async Task<bool> DeleteLocation(
        long id,
        [Service] IMediator mediator,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new DeleteLocationCommand(id), cancellationToken);
        return DeletedOrThrow(result);
    }

    public async Task<TaskItem> CreateTask(
        TaskItem input,
        [Service] IMediator mediator,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new CreateTaskCommand(input), cancellationToken);
        return ValueOrThrow(result);
    }

    public async Task<TaskItem?> UpdateTask(
        long id,
        TaskItem input,
        [Service] IMediator mediator,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new UpdateTaskCommand(id, input), cancellationToken);
        return NullableValueOrThrow(result);
    }

    public async Task<bool> DeleteTask(
        long id,
        [Service] IMediator mediator,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new DeleteTaskCommand(id), cancellationToken);
        return DeletedOrThrow(result);
    }

    private static T ValueOrThrow<T>(CommandResult<T> result)
        where T : class =>
        result.Status switch {
            CommandStatus.Succeeded when result.Value is not null => result.Value,
            CommandStatus.NotFound => throw new InvalidOperationException("Entity not found."),
            CommandStatus.Rejected => throw new ArgumentException(result.Error),
            _ => throw new InvalidOperationException($"Unhandled command status: {result.Status}")
        };

    private static T? NullableValueOrThrow<T>(CommandResult<T> result)
        where T : class =>
        result.Status switch {
            CommandStatus.Succeeded => result.Value,
            CommandStatus.NotFound => null,
            CommandStatus.Rejected => throw new ArgumentException(result.Error),
            _ => throw new InvalidOperationException($"Unhandled command status: {result.Status}")
        };

    private static bool DeletedOrThrow(CommandResult result) =>
        result.Status switch {
            CommandStatus.Succeeded => true,
            CommandStatus.NotFound => false,
            CommandStatus.Rejected => throw new ArgumentException(result.Error),
            _ => throw new InvalidOperationException($"Unhandled command status: {result.Status}")
        };
}
