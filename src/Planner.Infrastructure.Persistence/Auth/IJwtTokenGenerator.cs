using System.Security.Claims;

namespace Planner.Infrastructure.Persistence.Auth;

public interface IJwtTokenGenerator {
    string GenerateToken(
        long userId,
        Guid tenantId,
        string role);
}
