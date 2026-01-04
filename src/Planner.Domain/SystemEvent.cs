namespace Planner.Domain;

public class SystemEvent {
    public long Id { get; set; }
    public Guid TenantId { get; init; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsError { get; set; }
}
