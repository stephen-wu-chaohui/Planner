namespace Planner.Application;

public interface ITenantContext {
    Guid TenantId { get; }
}

public sealed class StaticTenantContext : ITenantContext {
    public Guid TenantId => Guid.Parse("07B6C438-8B21-4B11-A0F3-9CFD1A2711FC");
}
