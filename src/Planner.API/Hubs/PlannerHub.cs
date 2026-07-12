using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Planner.Application.Persistence;
using Planner.API.Auth;

namespace Planner.API.Hubs;

[Authorize]
public sealed class PlannerHub(
    IPlannerDbContext db,
    IMemoryCache cache,
    ILogger<PlannerHub> logger) : Hub {
    public const string Route = "/hubs/planner";

    public static string TenantGroup(Guid tenantId) => $"tenant:{tenantId:N}";

    public override async Task OnConnectedAsync() {
        var tenantId = await ResolveTenantIdAsync();
        if (tenantId == Guid.Empty) {
            logger.LogWarning(
                "Rejecting SignalR connection {ConnectionId} because no tenant could be resolved.",
                Context.ConnectionId);
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, TenantGroup(tenantId));
        await base.OnConnectedAsync();
    }

    private async Task<Guid> ResolveTenantIdAsync() {
        var email = EntraUserIdentity.ResolveLogin(Context.User);
        if (string.IsNullOrWhiteSpace(email)) {
            return Guid.Empty;
        }

        var cacheKey = $"TenantMapping_{email.ToLowerInvariant()}";
        if (cache.TryGetValue(cacheKey, out Guid cachedTenantId)) {
            return cachedTenantId;
        }

        var userRecord = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, Context.ConnectionAborted);
        if (userRecord is null) {
            return Guid.Empty;
        }

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
        cache.Set(cacheKey, userRecord.TenantId, cacheOptions);

        return userRecord.TenantId;
    }
}
