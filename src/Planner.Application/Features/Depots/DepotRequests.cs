using MediatR;
using Microsoft.EntityFrameworkCore;
using Planner.Application.Caching;
using Planner.Application.CQRS;
using Planner.Application.Mappings;
using Planner.Application;
using Planner.Contracts.API;

namespace Planner.Application.Features.Depots;

public sealed record GetDepotsQuery : IRequest<List<DepotDto>>;

public sealed record GetDepotByIdQuery(long Id) : IRequest<DepotDto?>;

public sealed record CreateDepotCommand(DepotDto Depot) : IRequest<CommandResult<DepotDto>>;

public sealed record UpdateDepotCommand(long Id, DepotDto Depot) : IRequest<CommandResult<DepotDto>>;

public sealed record DeleteDepotCommand(long Id) : IRequest<CommandResult>;

public sealed class DepotQueryHandler(IPlannerDataCenter dataCenter, ITenantContext tenant) :
    IRequestHandler<GetDepotsQuery, List<DepotDto>>,
    IRequestHandler<GetDepotByIdQuery, DepotDto?> {
    public async Task<List<DepotDto>> Handle(
        GetDepotsQuery query,
        CancellationToken cancellationToken) =>
        await dataCenter.GetOrFetchAsync(
            CacheKeys.DepotsList(tenant.TenantId),
            async () => await dataCenter.DbContext.Depots
                .AsNoTracking()
                .Where(d => d.TenantId == tenant.TenantId)
                .Include(d => d.Location)
                .Select(d => d.ToDto())
                .ToListAsync(cancellationToken),
            cancellationToken: cancellationToken) ?? [];

    public async Task<DepotDto?> Handle(
        GetDepotByIdQuery query,
        CancellationToken cancellationToken) =>
        await dataCenter.GetOrFetchAsync(
            CacheKeys.DepotById(query.Id, tenant.TenantId),
            async () => await dataCenter.DbContext.Depots
                .AsNoTracking()
                .Where(d => d.TenantId == tenant.TenantId)
                .Include(d => d.Location)
                .Where(d => d.Id == query.Id)
                .Select(d => d.ToDto())
                .FirstOrDefaultAsync(cancellationToken),
            cancellationToken: cancellationToken);
}

public sealed class DepotCommandHandler(IPlannerDataCenter dataCenter, ITenantContext tenant) :
    IRequestHandler<CreateDepotCommand, CommandResult<DepotDto>>,
    IRequestHandler<UpdateDepotCommand, CommandResult<DepotDto>>,
    IRequestHandler<DeleteDepotCommand, CommandResult> {
    public async Task<CommandResult<DepotDto>> Handle(
        CreateDepotCommand command,
        CancellationToken cancellationToken) {
        var entity = command.Depot.ToDomain(tenant.TenantId);
        dataCenter.DbContext.Depots.Add(entity);
        await dataCenter.DbContext.SaveChangesAsync(cancellationToken);
        await RemoveCacheAsync(entity.Id, cancellationToken);

        return CommandResult<DepotDto>.Succeeded(entity.ToDto());
    }

    public async Task<CommandResult<DepotDto>> Handle(
        UpdateDepotCommand command,
        CancellationToken cancellationToken) {
        if (command.Id != command.Depot.Id) {
            return CommandResult<DepotDto>.Rejected("ID mismatch");
        }

        var existing = await dataCenter.DbContext.Depots
            .Where(d => d.TenantId == tenant.TenantId)
            .FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken);
        if (existing is null) {
            return CommandResult<DepotDto>.NotFound();
        }

        var updated = command.Depot.ToDomain(tenant.TenantId);
        dataCenter.DbContext.Entry(existing).CurrentValues.SetValues(updated);
        await dataCenter.DbContext.SaveChangesAsync(cancellationToken);
        await RemoveCacheAsync(command.Id, cancellationToken);

        return CommandResult<DepotDto>.Succeeded(command.Depot);
    }

    public async Task<CommandResult> Handle(
        DeleteDepotCommand command,
        CancellationToken cancellationToken) {
        var entity = await dataCenter.DbContext.Depots
            .Where(d => d.TenantId == tenant.TenantId)
            .FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken);
        if (entity is null) {
            return CommandResult.NotFound();
        }

        dataCenter.DbContext.Depots.Remove(entity);
        await dataCenter.DbContext.SaveChangesAsync(cancellationToken);
        await RemoveCacheAsync(command.Id, cancellationToken);

        return CommandResult.Succeeded();
    }

    private Task RemoveCacheAsync(long depotId, CancellationToken cancellationToken) =>
        dataCenter.RemoveCacheKeysAsync(
            cancellationToken,
            CacheKeys.DepotsList(tenant.TenantId),
            CacheKeys.DepotById(depotId, tenant.TenantId),
            CacheKeys.ConfigInit(tenant.TenantId),
            CacheKeys.TenantMetadata(tenant.TenantId),
            CacheKeys.VehiclesList(tenant.TenantId));
}
