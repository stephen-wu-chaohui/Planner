namespace Planner.BlazorApp.Auth;

public interface IJwtTokenStore {
    string? AccessToken { get; }
    bool IsAuthenticated { get; }
    bool IsExpired();

    void Set(string token);
    void Clear();
}
