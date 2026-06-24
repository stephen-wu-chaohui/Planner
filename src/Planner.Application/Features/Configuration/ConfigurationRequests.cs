using MediatR;
using Microsoft.EntityFrameworkCore;
using Planner.Application.Caching;
using Planner.Application.CQRS;
using Planner.Application.Mappings;
using Planner.Application;
using Planner.Contracts.API;

namespace Planner.Application.Features.Configuration;

public sealed record GetClientConfigurationQuery : IRequest<TenantInfo?>;

public sealed class ConfigurationQueryHandler(IPlannerDataCenter dataCenter, ITenantContext tenant) :
    IRequestHandler<GetClientConfigurationQuery, TenantInfo?> {
    public async Task<TenantInfo?> Handle(
        GetClientConfigurationQuery query,
        CancellationToken cancellationToken) =>
        await dataCenter.GetOrFetchAsync(
            CacheKeys.ConfigInit(tenant.TenantId),
            async () => {
                var mainDepot = await dataCenter.DbContext.Depots
                    .AsNoTracking()
                    .Where(d => d.TenantId == tenant.TenantId)
                    .Include(d => d.Location)
                    .FirstOrDefaultAsync(cancellationToken);

                if (mainDepot is null) {
                    return null;
                }

                return await dataCenter.DbContext.Tenants
                    .AsNoTracking()
                    .Where(t => t.Id == tenant.TenantId)
                    .Select(t => new TenantInfo(t.Id, t.Name, mainDepot.ToDto()))
                    .FirstOrDefaultAsync(cancellationToken);
            },
            cancellationToken: cancellationToken);
}
