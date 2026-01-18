using System.Text.Json;

namespace Planner.BlazorApp.Auth;

/// <summary>
/// Manages JWT token storage with persistence across browser sessions.
/// 
/// Security features:
/// - Uses ProtectedLocalStorage for encrypted browser storage (OWASP compliant)
/// - Tokens are encrypted before storing in browser local storage
/// - Token expiration is validated before each API call
/// - Tokens are automatically cleared on 401 Unauthorized responses
/// - Logout properly clears tokens from both memory and storage
/// 
/// Token lifecycle:
/// 1. InitializeAsync() loads token from storage on app startup
/// 2. SetAsync() stores new token after successful login
/// 3. IsExpired() checks token expiration before API calls
/// 4. ClearAsync() removes token on logout or expiration
/// </summary>
public sealed class JwtTokenStore : IJwtTokenStore
{
    private const string TokenStorageKey = "planner_jwt_token";
    private readonly IProtectedStorage _protectedStorage;
    private bool _initialized;

    public JwtTokenStore(IProtectedStorage protectedStorage)
    {
        _protectedStorage = protectedStorage;
    }

    public string? AccessToken { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);

    public bool IsExpired() {
        if (string.IsNullOrEmpty(AccessToken)) return true;

        try {
            // JWT format is Header.Payload.Signature
            var parts = AccessToken.Split('.');
            if (parts.Length < 2) return true;

            var payload = parts[1];
            // Standard Base64 padding fix
            payload = payload.Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4) {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var jsonBytes = Convert.FromBase64String(payload);
            using var jsonDoc = JsonDocument.Parse(jsonBytes);

            if (jsonDoc.RootElement.TryGetProperty("exp", out var expElement)) {
                var expUnix = expElement.GetInt64();
                var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;

                // Returns true if current time is past expiration
                return expiresAt < DateTime.UtcNow;
            }
        } catch {
            return true;
        }
        return true;
    }


    public async Task InitializeAsync()
    {
        if (_initialized) return;

        try
        {
            var result = await _protectedStorage.GetAsync<string>(TokenStorageKey);
            if (result.Success && !string.IsNullOrEmpty(result.Value))
            {
                AccessToken = result.Value;
            }
        }
        catch
        {
            // If storage access fails, continue without loading token
        }
        finally
        {
            _initialized = true;
        }
    }

    public async Task SetAsync(string token)
    {
        AccessToken = token;
        try
        {
            await _protectedStorage.SetAsync(TokenStorageKey, token);
        }
        catch
        {
            // If storage fails, token is still available in memory for current session
        }
    }

    public async Task ClearAsync()
    {
        AccessToken = null;
        try
        {
            await _protectedStorage.DeleteAsync(TokenStorageKey);
        }
        catch
        {
            // If deletion fails, token is still cleared from memory
        }
    }

    // Legacy synchronous methods for backward compatibility
    public void Set(string token)
    {
        AccessToken = token;
        // Fire and forget for storage persistence
        _ = Task.Run(async () =>
        {
            try
            {
                await _protectedStorage.SetAsync(TokenStorageKey, token);
            }
            catch
            {
                // Silently fail - token is available in memory
            }
        });
    }

    public void Clear()
    {
        AccessToken = null;
        // Fire and forget for storage cleanup
        _ = Task.Run(async () =>
        {
            try
            {
                await _protectedStorage.DeleteAsync(TokenStorageKey);
            }
            catch
            {
                // Silently fail - token is cleared from memory
            }
        });
    }
}