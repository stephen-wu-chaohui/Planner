namespace Planner.Application;

public interface ITenantContext {
    Guid TenantId { get; }
}

public sealed class StaticTenantContext : ITenantContext {
    public Guid TenantId => Guid.Parse("40E7143C-EAC0-46BE-B72A-3A8C787D0A32");
}
