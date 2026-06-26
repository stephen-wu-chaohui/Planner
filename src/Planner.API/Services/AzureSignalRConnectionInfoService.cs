using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Planner.Contracts.OptimizationRuns;

namespace Planner.API.Services;

public interface IAzureSignalRConnectionInfoService {
    SignalRConnectionInfoDto? CreateConnectionInfo(string userId);
}

public sealed class AzureSignalRConnectionInfoService(
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<AzureSignalRConnectionInfoService> logger) : IAzureSignalRConnectionInfoService {
    private const string DefaultHubName = "planner";

    public SignalRConnectionInfoDto? CreateConnectionInfo(string userId) {
        if (!environment.IsProduction()) {
            logger.LogDebug("Azure SignalR negotiation is disabled outside Production. Use the native API SignalR hub.");
            return null;
        }

        var connectionString = configuration["SignalR:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString)) {
            logger.LogWarning("SignalR:ConnectionString is not configured. Realtime notifications are disabled.");
            return null;
        }

        var values = ParseConnectionString(connectionString);
        if (!values.TryGetValue("Endpoint", out var endpoint) || string.IsNullOrWhiteSpace(endpoint)) {
            logger.LogWarning("SignalR:ConnectionString is missing Endpoint. Realtime notifications are disabled.");
            return null;
        }

        if (!values.TryGetValue("AccessKey", out var accessKey) || string.IsNullOrWhiteSpace(accessKey)) {
            logger.LogWarning("SignalR:ConnectionString is missing AccessKey. Realtime notifications are disabled.");
            return null;
        }

        if (!TryDecodeAccessKey(accessKey, out var accessKeyBytes)) {
            logger.LogWarning("SignalR:ConnectionString AccessKey is not a valid Azure SignalR key. Realtime notifications are disabled.");
            return null;
        }

        var hubName = configuration["SignalR:HubName"] ?? DefaultHubName;
        var url = $"{endpoint.TrimEnd('/')}/client/?hub={Uri.EscapeDataString(hubName)}";
        var token = CreateJwt(url, accessKeyBytes, userId);
        return new SignalRConnectionInfoDto(url, token);
    }

    private static string CreateJwt(string audience, byte[] accessKey, string userId) {
        var key = new SymmetricSecurityKey(accessKey);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("nameid", userId),
            new Claim("sub", userId)
        ]);

        var token = new JwtSecurityTokenHandler().CreateJwtSecurityToken(
            issuer: null,
            audience: audience,
            subject: identity,
            notBefore: now,
            expires: now.AddHours(1),
            issuedAt: now,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static bool TryDecodeAccessKey(string accessKey, out byte[] accessKeyBytes) {
        try {
            accessKeyBytes = Convert.FromBase64String(accessKey);
            return accessKeyBytes.Length > 32;
        } catch (FormatException) {
            accessKeyBytes = [];
            return false;
        }
    }

    private static Dictionary<string, string> ParseConnectionString(string connectionString) =>
        connectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);
}
