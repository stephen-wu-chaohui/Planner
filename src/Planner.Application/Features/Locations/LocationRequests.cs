using MediatR;
using Microsoft.EntityFrameworkCore;
using Planner.Application.Caching;
using Planner.Application.CQRS;
using Planner.Application.Mappings;
using Planner.Contracts.API;
using Planner.Application;

namespace Planner.Application.Features.Locations;

public sealed record GetLocationsQuery : IRequest<List<LocationDto>>;

public sealed record GetLocationByIdQuery(long Id) : IRequest<LocationDto?>;

public sealed record CreateLocationCommand(LocationDto Location) : IRequest<CommandResult<LocationDto>>;

public sealed record UpdateLocationCommand(long Id, LocationDto Location) : IRequest<CommandResult<LocationDto>>;

public sealed record DeleteLocationCommand(long Id) : IRequest<CommandResult>;

public sealed class LocationQueryHandler(IPlannerDataCenter dataCenter) :
    IRequestHandler<GetLocationsQuery, List<LocationDto>>,
    IRequestHandler<GetLocationByIdQuery, LocationDto?> {
    public async Task<List<LocationDto>> Handle(
        GetLocationsQuery query,
        CancellationToken cancellationToken) =>
        await dataCenter.GetOrFetchAsync(
            CacheKeys.LocationsList(),
            async () => await dataCenter.DbContext.Locations
                .AsNoTracking()
                .Select(l => l.ToDto())
                .ToListAsync(cancellationToken),
            cancellationToken: cancellationToken) ?? [];

    public async Task<LocationDto?> Handle(
        GetLocationByIdQuery query,
        CancellationToken cancellationToken) =>
        await dataCenter.GetOrFetchAsync(
            CacheKeys.LocationById(query.Id),
            async () => await dataCenter.DbContext.Locations
                .AsNoTracking()
                .Where(l => l.Id == query.Id)
                .Select(l => l.ToDto())
                .FirstOrDefaultAsync(cancellationToken),
            cancellationToken: cancellationToken);
}

public sealed class LocationCommandHandler(IPlannerDataCenter dataCenter) :
    IRequestHandler<CreateLocationCommand, CommandResult<LocationDto>>,
    IRequestHandler<UpdateLocationCommand, CommandResult<LocationDto>>,
    IRequestHandler<DeleteLocationCommand, CommandResult> {
    public async Task<CommandResult<LocationDto>> Handle(
        CreateLocationCommand command,
        CancellationToken cancellationToken) {
        var entity = command.Location.ToDomain();
        dataCenter.DbContext.Locations.Add(entity);
        await dataCenter.DbContext.SaveChangesAsync(cancellationToken);
        await RemoveCacheAsync(entity.Id, cancellationToken);

        return CommandResult<LocationDto>.Succeeded(entity.ToDto());
    }

    public async Task<CommandResult<LocationDto>> Handle(
        UpdateLocationCommand command,
        CancellationToken cancellationToken) {
        if (command.Id != command.Location.Id) {
            return CommandResult<LocationDto>.Rejected("ID mismatch");
        }

        var existing = await dataCenter.DbContext.Locations
            .FirstOrDefaultAsync(l => l.Id == command.Id, cancellationToken);
        if (existing is null) {
            return CommandResult<LocationDto>.NotFound();
        }

        var updated = command.Location.ToDomain();
        dataCenter.DbContext.Entry(existing).CurrentValues.SetValues(updated);
        await dataCenter.DbContext.SaveChangesAsync(cancellationToken);
        await RemoveCacheAsync(command.Id, cancellationToken);

        return CommandResult<LocationDto>.Succeeded(command.Location);
    }

    public async Task<CommandResult> Handle(
        DeleteLocationCommand command,
        CancellationToken cancellationToken) {
        var entity = await dataCenter.DbContext.Locations
            .FirstOrDefaultAsync(l => l.Id == command.Id, cancellationToken);
        if (entity is null) {
            return CommandResult.NotFound();
        }

        dataCenter.DbContext.Locations.Remove(entity);
        await dataCenter.DbContext.SaveChangesAsync(cancellationToken);
        await RemoveCacheAsync(command.Id, cancellationToken);

        return CommandResult.Succeeded();
    }

    private Task RemoveCacheAsync(long locationId, CancellationToken cancellationToken) =>
        dataCenter.RemoveCacheKeysAsync(
            cancellationToken,
            CacheKeys.LocationsList(),
            CacheKeys.LocationById(locationId));
}
