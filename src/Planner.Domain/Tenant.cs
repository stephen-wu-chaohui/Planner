
public sealed class Tenant {
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

