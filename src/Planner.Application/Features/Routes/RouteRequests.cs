using MediatR;
using Microsoft.EntityFrameworkCore;
using Planner.Application.Caching;
using Planner.Application.CQRS;
using Planner.Application;
using Planner.Domain;
using DomainRoute = Planner.Domain.Route;

namespace Planner.Application.Features.Routes;

public sealed record GetRoutesQuery : IRequest<List<DomainRoute>>;

public sealed record GetRouteByIdQuery(long Id) : IRequest<DomainRoute?>;

public sealed record CreateRouteCommand(DomainRoute Route) : IRequest<CommandResult<DomainRoute>>;

public sealed record UpdateRouteCommand(long Id, DomainRoute Route) : IRequest<CommandResult<DomainRoute>>;

public sealed record DeleteRouteCommand(long Id) : IRequest<CommandResult>;

public sealed class RouteQueryHandler(IPlannerDataCenter dataCenter, ITenantContext tenant) :
    IRequestHandler<GetRoutesQuery, List<DomainRoute>>,
    IRequestHandler<GetRouteByIdQuery, DomainRoute?> {
    public async Task<List<DomainRoute>> Handle(
        GetRoutesQuery query,
        CancellationToken cancellationToken) =>
        await dataCenter.GetOrFetchAsync(
            CacheKeys.RoutesList(tenant.TenantId),
            async () => await dataCenter.DbContext.Set<DomainRoute>()
                .AsNoTracking()
                .Where(r => r.TenantId == tenant.TenantId)
                .ToListAsync(cancellationToken),
            cancellationToken: cancellationToken) ?? [];

    public async Task<DomainRoute?> Handle(
        GetRouteByIdQuery query,
        CancellationToken cancellationToken) =>
        await dataCenter.GetOrFetchAsync(
            CacheKeys.RouteById(query.Id, tenant.TenantId),
            async () => await dataCenter.DbContext.Set<DomainRoute>()
                .AsNoTracking()
                .Where(r => r.TenantId == tenant.TenantId)
                .FirstOrDefaultAsync(r => r.Id == query.Id, cancellationToken),
            cancellationToken: cancellationToken);
}

public sealed class RouteCommandHandler(IPlannerDataCenter dataCenter, ITenantContext tenant) :
    IRequestHandler<CreateRouteCommand, CommandResult<DomainRoute>>,
    IRequestHandler<UpdateRouteCommand, CommandResult<DomainRoute>>,
    IRequestHandler<DeleteRouteCommand, CommandResult> {
    public async Task<CommandResult<DomainRoute>> Handle(
        CreateRouteCommand command,
        CancellationToken cancellationToken) {
        var entity = ForTenant(command.Route);
        dataCenter.DbContext.Set<DomainRoute>().Add(entity);
        await dataCenter.DbContext.SaveChangesAsync(cancellationToken);
        await RemoveCacheAsync(entity.Id, cancellationToken);

        return CommandResult<DomainRoute>.Succeeded(entity);
    }

    public async Task<CommandResult<DomainRoute>> Handle(
        UpdateRouteCommand command,
        CancellationToken cancellationToken) {
        if (command.Id != command.Route.Id) {
            return CommandResult<DomainRoute>.Rejected("ID mismatch");
        }

        var existing = await dataCenter.DbContext.Set<DomainRoute>()
            .Where(r => r.TenantId == tenant.TenantId)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);
        if (existing is null) {
            return CommandResult<DomainRoute>.NotFound();
        }

        var updated = ForTenant(command.Route);
        dataCenter.DbContext.Entry(existing).CurrentValues.SetValues(updated);
        await dataCenter.DbContext.SaveChangesAsync(cancellationToken);
        await RemoveCacheAsync(command.Id, cancellationToken);

        return CommandResult<DomainRoute>.Succeeded(updated);
    }

    public async Task<CommandResult> Handle(
        DeleteRouteCommand command,
        CancellationToken cancellationToken) {
        var entity = await dataCenter.DbContext.Set<DomainRoute>()
            .Where(r => r.TenantId == tenant.TenantId)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);
        if (entity is null) {
            return CommandResult.NotFound();
        }

        dataCenter.DbContext.Set<DomainRoute>().Remove(entity);
        await dataCenter.DbContext.SaveChangesAsync(cancellationToken);
        await RemoveCacheAsync(command.Id, cancellationToken);

        return CommandResult.Succeeded();
    }

    private DomainRoute ForTenant(DomainRoute route) =>
        new() {
            Id = route.Id,
            TenantId = tenant.TenantId,
            OptimizationRunId = route.OptimizationRunId,
            VehicleId = route.VehicleId,
            TotalDistanceKm = route.TotalDistanceKm,
            TotalCost = route.TotalCost,
            Stops = route.Stops
        };

    private Task RemoveCacheAsync(long routeId, CancellationToken cancellationToken) =>
        dataCenter.RemoveCacheKeysAsync(
            cancellationToken,
            CacheKeys.RoutesList(tenant.TenantId),
            CacheKeys.RouteById(routeId, tenant.TenantId));
}
