using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Planner.Infrastructure.Auth;

public sealed class JwtTokenGenerator(IOptions<JwtOptions> options) : IJwtTokenGenerator {
    private readonly JwtOptions _options = options.Value;

    public string GenerateToken(
        long userId,
        Guid tenantId,
        string role) {
        var claims = new List<Claim>
        {
            // new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new("sub", userId.ToString()),
            new("tenant_id", tenantId.ToString()),
            new(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.SigningKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpirationInMinutes),
            signingCredentials: credentials);

        //return new JwtSecurityTokenHandler().WriteToken(token);
        try {
            var s = new JwtSecurityTokenHandler().WriteToken(token);
            return s.ToString();
        } catch(Exception ex) {
            return ex.Message;
        }
    }
}
