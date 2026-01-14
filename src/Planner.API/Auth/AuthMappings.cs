using Planner.Contracts.API.Auth;
using Planner.Domain;
using Planner.Infrastructure.Auth;

namespace Planner.API.Auth;

/// <summary>
/// Maps domain authentication models to transport DTOs.
/// </summary>
public static class AuthMappings {
    public static LoginResponse ToLoginResponse(this User user, IJwtTokenGenerator tokenGenerator) {
        var token = tokenGenerator.GenerateToken(
            userId: user.Id,
            tenantId: user.TenantId,
            role: user.Role);

        return new LoginResponse(token);
    }
}
