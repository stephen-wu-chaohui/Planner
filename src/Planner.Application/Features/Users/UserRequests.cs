using MediatR;
using Microsoft.EntityFrameworkCore;
using Planner.Application.Caching;
using Planner.Application.CQRS;
using Planner.Application;

namespace Planner.Application.Features.Users;

public sealed record UserSummary(string Email, string Role, DateTime CreatedAt);

public sealed record GetUsersQuery : IRequest<List<UserSummary>>;

public sealed class UserQueryHandler(IPlannerDataCenter dataCenter, ITenantContext tenant) :
    IRequestHandler<GetUsersQuery, List<UserSummary>> {
    public async Task<List<UserSummary>> Handle(
        GetUsersQuery query,
        CancellationToken cancellationToken) =>
        await dataCenter.GetOrFetchAsync(
            CacheKeys.UsersList(tenant.TenantId),
            async () => await dataCenter.DbContext.Users
                .AsNoTracking()
                .Where(u => u.TenantId == tenant.TenantId)
                .Select(u => new UserSummary(u.Email, u.Role, u.CreatedAt))
                .ToListAsync(cancellationToken),
            cancellationToken: cancellationToken) ?? [];
}
