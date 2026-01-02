namespace Planner.Domain;

public class UserAccount {
    public long Id { get; set; }
    public Guid TenantId { get; init; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SystemEvent {
    public long Id { get; set; }
    public Guid TenantId { get; init; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsError { get; set; }
}

public class TaskItem {
    public long Id { get; set; }
    public Guid TenantId { get; init; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; } = false;
}

