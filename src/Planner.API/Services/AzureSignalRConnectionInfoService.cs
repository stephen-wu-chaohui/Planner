using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Planner.Contracts.OptimizationRuns;

namespace Planner.API.Services;

public interface IAzureSignalRConnectionInfoService {
    SignalRConnectionInfoDto CreateConnectionInfo(string userId);
}

public sealed class AzureSignalRConnectionInfoService(IConfiguration configuration) : IAzureSignalRConnectionInfoService {
    private const string DefaultHubName = "planner";

    public SignalRConnectionInfoDto CreateConnectionInfo(string userId) {
        var connectionString = configuration["SignalR:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString)) {
            throw new InvalidOperationException("SignalR:ConnectionString is not configured.");
        }

        var values = ParseConnectionString(connectionString);
        if (!values.TryGetValue("Endpoint", out var endpoint) || string.IsNullOrWhiteSpace(endpoint)) {
            throw new InvalidOperationException("SignalR:ConnectionString is missing Endpoint.");
        }

        if (!values.TryGetValue("AccessKey", out var accessKey) || string.IsNullOrWhiteSpace(accessKey)) {
            throw new InvalidOperationException("SignalR:ConnectionString is missing AccessKey.");
        }

        var hubName = configuration["SignalR:HubName"] ?? DefaultHubName;
        var url = $"{endpoint.TrimEnd('/')}/client/?hub={Uri.EscapeDataString(hubName)}";
        var token = CreateJwt(url, accessKey, userId);
        return new SignalRConnectionInfoDto(url, token);
    }

    private static string CreateJwt(string audience, string accessKey, string userId) {
        var key = new SymmetricSecurityKey(Convert.FromBase64String(accessKey));
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

    private static Dictionary<string, string> ParseConnectionString(string connectionString) =>
        connectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);
}
