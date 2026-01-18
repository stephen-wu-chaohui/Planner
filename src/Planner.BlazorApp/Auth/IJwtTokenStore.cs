namespace Planner.BlazorApp.Auth;

public interface IJwtTokenStore {
    string? AccessToken { get; }
    bool IsAuthenticated { get; }
    bool IsExpired();

    Task InitializeAsync();
    Task SetAsync(string token);
    Task ClearAsync();

    // Legacy synchronous methods for backward compatibility
    void Set(string token);
    void Clear();
}
