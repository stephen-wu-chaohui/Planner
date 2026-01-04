using System.Text.Json;

namespace Planner.BlazorApp.Auth;

public sealed class JwtTokenStore : IJwtTokenStore
{
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


    public void Set(string token)
    {
        AccessToken = token;
    }
    public void Clear()
    {
        AccessToken = null;
    }
}