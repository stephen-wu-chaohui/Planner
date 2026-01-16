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
}
