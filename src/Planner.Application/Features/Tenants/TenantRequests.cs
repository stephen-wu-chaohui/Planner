using MediatR;
using Microsoft.EntityFrameworkCore;
using Planner.Application.Caching;
using Planner.Application.CQRS;
using Planner.Application.Mappings;
using Planner.Application;
using Planner.Contracts.API;

namespace Planner.Application.Features.Tenants;

public sealed record GetTenantMetadataQuery : IRequest<TenantDto?>;

public sealed class TenantQueryHandler(IPlannerDataCenter dataCenter, ITenantContext tenant) :
    IRequestHandler<GetTenantMetadataQuery, TenantDto?> {
    public async Task<TenantDto?> Handle(
        GetTenantMetadataQuery query,
        CancellationToken cancellationToken) =>
        await dataCenter.GetOrFetchAsync(
            CacheKeys.TenantMetadata(tenant.TenantId),
            async () => {
                var tenantEntity = await dataCenter.DbContext.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == tenant.TenantId, cancellationToken);

                if (tenantEntity is null) {
                    return null;
                }

                var mainDepot = await dataCenter.DbContext.Depots
                    .AsNoTracking()
                    .Where(d => d.TenantId == tenant.TenantId)
                    .FirstOrDefaultAsync(cancellationToken);

                return tenantEntity.ToDto(mainDepot?.Id);
            },
            cancellationToken: cancellationToken);
}
