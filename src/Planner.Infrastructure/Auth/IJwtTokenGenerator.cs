using System.Security.Claims;

namespace Planner.Infrastructure.Auth;

public interface IJwtTokenGenerator {
    string GenerateToken(
        long userId,
        Guid tenantId,
        string role);
}
