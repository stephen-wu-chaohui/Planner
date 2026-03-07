namespace Planner.API.Caching;

public static class CacheKeys {
    private static string Scope(Guid? tenantId) => tenantId.HasValue ? $"tenant:{tenantId.Value:N}" : "scope:all";

    public static string ConfigInit(Guid tenantId) => $"config:init:{Scope(tenantId)}";
    public static string TenantMetadata(Guid tenantId) => $"tenants:metadata:{Scope(tenantId)}";
    public static string UsersList() => "users:list:scope:all";

    public static string JobsList(Guid? tenantId = null) => $"jobs:list:{Scope(tenantId)}";
    public static string JobById(long id, Guid? tenantId = null) => $"jobs:item:{Scope(tenantId)}:{id}";

    public static string CustomersList(Guid? tenantId = null) => $"customers:list:{Scope(tenantId)}";
    public static string CustomerById(long id, Guid? tenantId = null) => $"customers:item:{Scope(tenantId)}:{id}";

    public static string VehiclesList(Guid? tenantId = null) => $"vehicles:list:{Scope(tenantId)}";
    public static string VehicleById(long id, Guid? tenantId = null) => $"vehicles:item:{Scope(tenantId)}:{id}";

    public static string DepotsList(Guid? tenantId = null) => $"depots:list:{Scope(tenantId)}";
    public static string DepotById(long id, Guid? tenantId = null) => $"depots:item:{Scope(tenantId)}:{id}";

    public static string LocationsList(Guid? tenantId = null) => $"locations:list:{Scope(tenantId)}";
    public static string LocationById(long id, Guid? tenantId = null) => $"locations:item:{Scope(tenantId)}:{id}";

    public static string RoutesList(Guid? tenantId = null) => $"routes:list:{Scope(tenantId)}";
    public static string RouteById(long id, Guid? tenantId = null) => $"routes:item:{Scope(tenantId)}:{id}";

    public static string TasksList(Guid? tenantId = null) => $"tasks:list:{Scope(tenantId)}";
    public static string TaskById(long id, Guid? tenantId = null) => $"tasks:item:{Scope(tenantId)}:{id}";
}
