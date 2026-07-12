using System.Security.Claims;

namespace Planner.API.Auth;

public static class EntraUserIdentity {
    private static readonly string[] LoginClaimTypes = [
        "preferred_username",
        "upn",
        ClaimTypes.Email,
        "email"
    ];

    public static string? ResolveLogin(ClaimsPrincipal? principal) {
        if (principal?.Identity?.IsAuthenticated != true) {
            return null;
        }

        foreach (var claimType in LoginClaimTypes) {
            var value = principal.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value)) {
                return value.Trim();
            }
        }

        return string.IsNullOrWhiteSpace(principal.Identity.Name)
            ? null
            : principal.Identity.Name.Trim();
    }
}
