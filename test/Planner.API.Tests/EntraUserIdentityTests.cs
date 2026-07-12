using System.Security.Claims;
using FluentAssertions;
using Planner.API.Auth;

namespace Planner.API.Tests;

public sealed class EntraUserIdentityTests {
    [Fact]
    public void ResolveLogin_PrefersPreferredUsernameOverDisplayName() {
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.Name, "Christchurch Admin"),
            new Claim("preferred_username", "christchurch.admin@plannerdemo.com")
        ], "Bearer");

        EntraUserIdentity.ResolveLogin(new ClaimsPrincipal(identity))
            .Should().Be("christchurch.admin@plannerdemo.com");
    }

    [Theory]
    [InlineData("upn")]
    [InlineData(ClaimTypes.Email)]
    [InlineData("email")]
    public void ResolveLogin_UsesSupportedEmailClaims(string claimType) {
        var identity = new ClaimsIdentity([
            new Claim(claimType, "christchurch.admin@plannerdemo.com")
        ], "Bearer");

        EntraUserIdentity.ResolveLogin(new ClaimsPrincipal(identity))
            .Should().Be("christchurch.admin@plannerdemo.com");
    }
}
