using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Planner.API.Controllers;
using Planner.Application;
using Planner.Contracts.API.Auth;
using Planner.Domain;
using Planner.Infrastructure.Auth;
using Planner.Infrastructure.Persistence;
using Xunit;

namespace Planner.API.EndToEndTests.Tests;

/// <summary>
/// End-to-end tests for HTTP-only cookie-based authentication flow.
/// Tests cover login, logout, and cookie management.
/// </summary>
public sealed class AuthenticationEndToEndTests : IDisposable {
    private readonly PlannerDbContext _db;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly Guid _testTenantId;
    private readonly TestTenantContext _tenantContext;

    public AuthenticationEndToEndTests() {
        _testTenantId = Guid.NewGuid();
        _tenantContext = new TestTenantContext(_testTenantId);

        // Setup in-memory database
        var options = new DbContextOptionsBuilder<PlannerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new PlannerDbContext(options, _tenantContext);

        // Setup JWT token generator
        var jwtOptions = Options.Create(new JwtOptions {
            Issuer = "test-issuer",
            Audience = "test-audience",
            SigningKey = "test-signing-key-at-least-32-chars-long-for-security",
            Secret = "test-secret-at-least-32-chars-long",
            ExpirationInMinutes = 60
        });
        _tokenGenerator = new JwtTokenGenerator(jwtOptions);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData() {
        var tenant = new Tenant {
            Id = _testTenantId,
            Name = "Test Tenant"
        };

        var user = new User {
            Id = 1,
            Email = "test.user@example.com",
            PasswordHash = "password123", // UNSAFE: Plain-text for testing only - use proper hashing in production
            Role = "Admin",
            TenantId = _testTenantId
        };

        _db.Tenants.Add(tenant);
        _db.Users.Add(user);
        _db.SaveChanges();
    }

    [Fact]
    public async Task Login_WithValidCredentials_SetsHttpOnlyCookie() {
        // Arrange
        var controller = CreateAuthController();
        var request = new LoginRequest("test.user@example.com", "password123");

        // Act
        var result = await controller.Login(request);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LoginResponse>().Subject;
        
        response.AccessToken.Should().NotBeNullOrEmpty();

        // Verify HTTP-only cookie was set
        var cookies = controller.Response.Headers["Set-Cookie"];
        cookies.Should().ContainSingle();
        var cookieHeader = cookies.ToString();
        
        cookieHeader.Should().Contain("planner-auth=");
        cookieHeader.Should().Contain("httponly");
        cookieHeader.Should().Contain("secure");
        cookieHeader.Should().Contain("samesite=strict");
    }

    [Fact]
    public async Task Login_WithInvalidEmail_ReturnsUnauthorized() {
        // Arrange
        var controller = CreateAuthController();
        var request = new LoginRequest("invalid@example.com", "password123");

        // Act
        var result = await controller.Login(request);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized() {
        // Arrange
        var controller = CreateAuthController();
        var request = new LoginRequest("test.user@example.com", "wrongpassword");

        // Act
        var result = await controller.Login(request);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_GeneratesValidJwtToken() {
        // Arrange
        var controller = CreateAuthController();
        var request = new LoginRequest("test.user@example.com", "password123");

        // Act
        var result = await controller.Login(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LoginResponse>().Subject;
        
        // Verify token can be parsed
        var token = response.AccessToken;
        token.Should().NotBeNullOrEmpty();
        
        // JWT format is three base64 segments separated by dots
        var parts = token.Split('.');
        parts.Should().HaveCount(3);
    }

    [Fact]
    public void Logout_ClearsAuthenticationCookie() {
        // Arrange
        var controller = CreateAuthControllerWithAuth();

        // Act
        var result = controller.Logout();

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        // Verify cookie deletion was requested
        var cookies = controller.Response.Headers["Set-Cookie"];
        var cookieHeader = cookies.ToString();
        
        // When deleting, the cookie should have an expired date
        cookieHeader.Should().Contain("planner-auth=");
        cookieHeader.Should().Contain("expires=");
    }

    [Fact]
    public async Task Diagnose_WithAuthentication_ReturnsUserClaims() {
        // Arrange
        var controller = CreateAuthControllerWithAuth();

        // Act
        var result = controller.Diagnose();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().NotBeNull();
    }

    private AuthController CreateAuthController() {
        var controller = new AuthController(_db, _tokenGenerator);
        
        // Mock HttpContext with Response
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext {
            HttpContext = httpContext
        };

        return controller;
    }

    private AuthController CreateAuthControllerWithAuth() {
        var controller = CreateAuthController();
        
        // Add authenticated user context
        var claims = new[] {
            new Claim("sub", "1"),
            new Claim("tenant_id", _testTenantId.ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        
        controller.ControllerContext.HttpContext.User = principal;
        
        return controller;
    }

    public void Dispose() {
        _db.Dispose();
    }

    private class TestTenantContext : ITenantContext {
        public Guid TenantId { get; }
        public bool IsSet => true;

        public TestTenantContext(Guid tenantId) {
            TenantId = tenantId;
        }

        public void SetTenant(Guid tenantId) {
            // Not needed for tests
        }
    }
}
