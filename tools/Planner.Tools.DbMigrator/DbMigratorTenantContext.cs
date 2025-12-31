using Planner.Application;

namespace Planner.Tools.DbMigrator;

internal sealed class DbMigratorTenantContext : ITenantContext {
    // Fixed, deterministic tenant for schema operations
    public Guid TenantId { get; } =
        Guid.Parse("00000000-0000-0000-0000-000000000001");
}
