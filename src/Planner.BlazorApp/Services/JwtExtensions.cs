using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Planner.BlazorApp.Services;

public static class JwtExtensions {
    public static string? GetRole(this string jwt) {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);
        return token.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
    }

    public static Guid? GetTenantId(this string jwt) {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);
        var value = token.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value;

        return Guid.TryParse(value, out var id)
            ? id
            : null;
    }
}
