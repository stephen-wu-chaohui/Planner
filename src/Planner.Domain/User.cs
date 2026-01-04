namespace Planner.Domain;

public class User {
    public long Id { get; set; }
    public Guid TenantId { get; init; }

    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "User"; // "Admin" | "User"

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
