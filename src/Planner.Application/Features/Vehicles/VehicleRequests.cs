using MediatR;
using Microsoft.EntityFrameworkCore;
using Planner.Application.Caching;
using Planner.Application.CQRS;
using Planner.Application.Mappings;
using Planner.Application;
using Planner.Contracts.API;

namespace Planner.Application.Features.Vehicles;

public sealed record VehicleListResult(List<VehicleDto> Items, int OmittedCount);

public sealed record GetVehiclesQuery : IRequest<VehicleListResult>;

public sealed record GetVehicleByIdQuery(long Id) : IRequest<VehicleDto?>;

public sealed record CreateVehicleCommand(VehicleDto Vehicle) : IRequest<CommandResult<VehicleDto>>;

public sealed record UpdateVehicleCommand(long Id, VehicleDto Vehicle) : IRequest<CommandResult<VehicleDto>>;

public sealed record DeleteVehicleCommand(long Id) : IRequest<CommandResult>;

public sealed class VehicleQueryHandler(IPlannerDataCenter dataCenter, ITenantContext tenant) :
    IRequestHandler<GetVehiclesQuery, VehicleListResult>,
    IRequestHandler<GetVehicleByIdQuery, VehicleDto?> {
    public async Task<VehicleListResult> Handle(
        GetVehiclesQuery query,
        CancellationToken cancellationToken) =>
        await dataCenter.GetOrFetchAsync(
            CacheKeys.VehiclesList(tenant.TenantId),
            async () => {
                var items = await dataCenter.DbContext.Vehicles
                    .AsNoTracking()
                    .Where(v => v.TenantId == tenant.TenantId)
                    .Include(v => v.StartDepot)
                    .ThenInclude(d => d!.Location)
                    .Include(v => v.EndDepot)
                    .ThenInclude(d => d!.Location)
                    .ToListAsync(cancellationToken);

                var valid = items
                    .Where(v =>
                        v.StartDepot is not null &&
                        v.EndDepot is not null &&
                        v.StartDepot.Location is not null &&
                        v.EndDepot.Location is not null)
                    .Select(v => v.ToDto())
                    .ToList();

                return new VehicleListResult(valid, items.Count - valid.Count);
            },
            cancellationToken: cancellationToken) ?? new VehicleListResult([], 0);

    public async Task<VehicleDto?> Handle(
        GetVehicleByIdQuery query,
        CancellationToken cancellationToken) =>
        await dataCenter.GetOrFetchAsync(
            CacheKeys.VehicleById(query.Id, tenant.TenantId),
            async () => await dataCenter.DbContext.Vehicles
                .AsNoTracking()
                .Where(v => v.TenantId == tenant.TenantId)
                .Include(v => v.StartDepot)
                .ThenInclude(d => d!.Location)
                .Include(v => v.EndDepot)
                .ThenInclude(d => d!.Location)
                .Where(v =>
                    v.Id == query.Id &&
                    v.StartDepot != null &&
                    v.EndDepot != null &&
                    v.StartDepot.Location != null &&
                    v.EndDepot.Location != null)
                .Select(v => v.ToDto())
                .FirstOrDefaultAsync(cancellationToken),
            cancellationToken: cancellationToken);
}

public sealed class VehicleCommandHandler(IPlannerDataCenter dataCenter, ITenantContext tenant) :
    IRequestHandler<CreateVehicleCommand, CommandResult<VehicleDto>>,
    IRequestHandler<UpdateVehicleCommand, CommandResult<VehicleDto>>,
    IRequestHandler<DeleteVehicleCommand, CommandResult> {
    public async Task<CommandResult<VehicleDto>> Handle(
        CreateVehicleCommand command,
        CancellationToken cancellationToken) {
        var entity = command.Vehicle.ToDomain(tenant.TenantId);
        dataCenter.DbContext.Vehicles.Add(entity);
        await dataCenter.DbContext.SaveChangesAsync(cancellationToken);
        await RemoveCacheAsync(entity.Id, cancellationToken);

        return CommandResult<VehicleDto>.Succeeded(entity.ToDto());
    }

    public async Task<CommandResult<VehicleDto>> Handle(
        UpdateVehicleCommand command,
        CancellationToken cancellationToken) {
        if (command.Id != command.Vehicle.Id) {
            return CommandResult<VehicleDto>.Rejected("ID mismatch");
        }

        var existing = await dataCenter.DbContext.Vehicles
            .Where(v => v.TenantId == tenant.TenantId)
            .FirstOrDefaultAsync(v => v.Id == command.Id, cancellationToken);
        if (existing is null) {
            return CommandResult<VehicleDto>.NotFound();
        }

        var updated = command.Vehicle.ToDomain(tenant.TenantId);
        dataCenter.DbContext.Entry(existing).CurrentValues.SetValues(updated);
        await dataCenter.DbContext.SaveChangesAsync(cancellationToken);
        await RemoveCacheAsync(command.Id, cancellationToken);

        return CommandResult<VehicleDto>.Succeeded(command.Vehicle);
    }

    public async Task<CommandResult> Handle(
        DeleteVehicleCommand command,
        CancellationToken cancellationToken) {
        var entity = await dataCenter.DbContext.Vehicles
            .Where(v => v.TenantId == tenant.TenantId)
            .FirstOrDefaultAsync(v => v.Id == command.Id, cancellationToken);
        if (entity is null) {
            return CommandResult.NotFound();
        }

        dataCenter.DbContext.Vehicles.Remove(entity);
        await dataCenter.DbContext.SaveChangesAsync(cancellationToken);
        await RemoveCacheAsync(command.Id, cancellationToken);

        return CommandResult.Succeeded();
    }

    private Task RemoveCacheAsync(long vehicleId, CancellationToken cancellationToken) =>
        dataCenter.RemoveCacheKeysAsync(
            cancellationToken,
            CacheKeys.VehiclesList(tenant.TenantId),
            CacheKeys.VehicleById(vehicleId, tenant.TenantId));
}
