using MediatR;
using HotChocolate.Authorization;
using Planner.Application.CQRS;
using Planner.Application.Features.Customers;
using Planner.Application.Features.Depots;
using Planner.Application.Features.Jobs;
using Planner.Application.Features.Locations;
using Planner.Application.Features.Routes;
using Planner.Application.Features.Tasks;
using Planner.Application.Features.Vehicles;
using Planner.Contracts.API;
using Planner.Domain;
using DomainRoute = Planner.Domain.Route;

namespace Planner.API.GraphQL;

public sealed class Query {
    [Authorize]
    public Task<List<JobDto>> GetJobs(
        [Service] IMediator mediator,
        CancellationToken cancellationToken) =>
        mediator.Send(new GetJobsQuery(), cancellationToken);

    [Authorize]
    public Task<JobDto?> GetJobById(
        long id,
        [Service] IMediator mediator,
        CancellationToken cancellationToken) =>
        mediator.Send(new GetJobByIdQuery(id), cancellationToken);

    [Authorize]
    public Task<List<CustomerDto>> GetCustomers(
        [Service] IMediator mediator,
        CancellationToken cancellationToken) =>
        mediator.Send(new GetCustomersQuery(), cancellationToken);

    [Authorize]
    public Task<CustomerDto?> GetCustomerById(
        long id,
        [Service] IMediator mediator,
        CancellationToken cancellationToken) =>
        mediator.Send(new GetCustomerByIdQuery(id), cancellationToken);

    [Authorize]
    public async Task<List<VehicleDto>> GetVehicles(
        [Service] IMediator mediator,
        CancellationToken cancellationToken) {
        var result = await mediator.Send(new GetVehiclesQuery(), cancellationToken);
        return result.Items;
    }

    [Authorize]
    public Task<VehicleDto?> GetVehicleById(
        long id,
        [Service] IMediator mediator,
        CancellationToken cancellationToken) =>
        mediator.Send(new GetVehicleByIdQuery(id), cancellationToken);

    [Authorize]
    public Task<List<DepotDto>> GetDepots(
        [Service] IMediator mediator,
        CancellationToken cancellationToken) =>
        mediator.Send(new GetDepotsQuery(), cancellationToken);

    [Authorize]
    public Task<DepotDto?> GetDepotById(
        long id,
        [Service] IMediator mediator,
        CancellationToken cancellationToken) =>
        mediator.Send(new GetDepotByIdQuery(id), cancellationToken);

    public Task<List<LocationDto>> GetLocations(
        [Service] IMediator mediator,
        CancellationToken cancellationToken) =>
        mediator.Send(new GetLocationsQuery(), cancellationToken);

    public Task<LocationDto?> GetLocationById(
        long id,
        [Service] IMediator mediator,
        CancellationToken cancellationToken) =>
        mediator.Send(new GetLocationByIdQuery(id), cancellationToken);

    [Authorize]
    public Task<List<DomainRoute>> GetRoutes(
        [Service] IMediator mediator,
        CancellationToken cancellationToken) =>
        mediator.Send(new GetRoutesQuery(), cancellationToken);

    [Authorize]
    public Task<DomainRoute?> GetRouteById(
        long id,
        [Service] IMediator mediator,
        CancellationToken cancellationToken) =>
        mediator.Send(new GetRouteByIdQuery(id), cancellationToken);

    public Task<List<TaskItem>> GetTasks(
        [Service] IMediator mediator,
        CancellationToken cancellationToken) =>
        mediator.Send(new GetTasksQuery(), cancellationToken);

    public Task<TaskItem?> GetTaskById(
        long id,
        [Service] IMediator mediator,
        CancellationToken cancellationToken) =>
        mediator.Send(new GetTaskByIdQuery(id), cancellationToken);
}
