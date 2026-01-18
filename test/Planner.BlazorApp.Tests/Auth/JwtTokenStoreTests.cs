using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Planner.BlazorApp.Auth;
using Xunit;

namespace Planner.BlazorApp.Tests.Auth;

public class JwtTokenStoreTests
{
    [Fact]
    public void IsExpired_WithNullToken_ReturnsTrue()
    {
        // Arrange
        var mockStorage = new Mock<IProtectedStorage>();
        var store = new JwtTokenStore(mockStorage.Object);

        // Act
        var result = store.IsExpired();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WithEmptyToken_ReturnsTrue()
    {
        // Arrange
        var mockStorage = new Mock<IProtectedStorage>();
        var store = new JwtTokenStore(mockStorage.Object);
        store.Set("");

        // Act
        var result = store.IsExpired();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_WithNullToken_ReturnsFalse()
    {
        // Arrange
        var mockStorage = new Mock<IProtectedStorage>();
        var store = new JwtTokenStore(mockStorage.Object);

        // Act
        var result = store.IsAuthenticated;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_WithToken_ReturnsTrue()
    {
        // Arrange
        var mockStorage = new Mock<IProtectedStorage>();
        var store = new JwtTokenStore(mockStorage.Object);
        var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwiZXhwIjo5OTk5OTk5OTk5fQ.signature";
        
        // Act
        store.Set(token);

        // Assert
        store.IsAuthenticated.Should().BeTrue();
        store.AccessToken.Should().Be(token);
    }

    [Fact]
    public void Clear_RemovesToken()
    {
        // Arrange
        var mockStorage = new Mock<IProtectedStorage>();
        var store = new JwtTokenStore(mockStorage.Object);
        var token = "test.token.here";
        store.Set(token);

        // Act
        store.Clear();

        // Assert
        store.IsAuthenticated.Should().BeFalse();
        store.AccessToken.Should().BeNull();
    }

    [Fact]
    public void IsExpired_WithExpiredToken_ReturnsTrue()
    {
        // Arrange
        var mockStorage = new Mock<IProtectedStorage>();
        var store = new JwtTokenStore(mockStorage.Object);
        // Token with exp claim in the past (Unix timestamp: 1000000000 = Sep 8, 2001)
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwiZXhwIjoxMDAwMDAwMDAwfQ.signature";
        store.Set(expiredToken);

        // Act
        var result = store.IsExpired();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WithValidToken_ReturnsFalse()
    {
        // Arrange
        var mockStorage = new Mock<IProtectedStorage>();
        var store = new JwtTokenStore(mockStorage.Object);
        // Token with exp claim in the far future (Unix timestamp: 9999999999)
        var validToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwiZXhwIjo5OTk5OTk5OTk5fQ.signature";
        store.Set(validToken);

        // Act
        var result = store.IsExpired();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task InitializeAsync_LoadsTokenFromStorage()
    {
        // Arrange
        var mockStorage = new Mock<IProtectedStorage>();
        var existingToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwiZXhwIjo5OTk5OTk5OTk5fQ.signature";
        
        // Create a result using reflection or mock the behavior
        mockStorage.Setup(s => s.GetAsync<string>("planner_jwt_token"))
            .Returns(Task.FromResult(CreateStorageResult(true, existingToken)));
        
        var store = new JwtTokenStore(mockStorage.Object);

        // Act
        await store.InitializeAsync();

        // Assert
        store.AccessToken.Should().Be(existingToken);
        store.IsAuthenticated.Should().BeTrue();
    }

    private static ProtectedBrowserStorageResult<T> CreateStorageResult<T>(bool success, T value)
    {
        // ProtectedBrowserStorageResult is a readonly struct, so we need to use Activator
        var resultType = typeof(ProtectedBrowserStorageResult<T>);
        var instance = Activator.CreateInstance(resultType, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new object[] { success, value }, null);
        return (ProtectedBrowserStorageResult<T>)instance!;
    }

    [Fact]
    public async Task SetAsync_StoresTokenInStorage()
    {
        // Arrange
        var mockStorage = new Mock<IProtectedStorage>();
        var store = new JwtTokenStore(mockStorage.Object);
        var token = "test.token.here";

        // Act
        await store.SetAsync(token);

        // Assert
        mockStorage.Verify(s => s.SetAsync("planner_jwt_token", token), Times.Once);
        store.AccessToken.Should().Be(token);
    }

    [Fact]
    public async Task ClearAsync_RemovesTokenFromStorage()
    {
        // Arrange
        var mockStorage = new Mock<IProtectedStorage>();
        var store = new JwtTokenStore(mockStorage.Object);
        var token = "test.token.here";
        await store.SetAsync(token);

        // Act
        await store.ClearAsync();

        // Assert
        mockStorage.Verify(s => s.DeleteAsync("planner_jwt_token"), Times.Once);
        store.AccessToken.Should().BeNull();
        store.IsAuthenticated.Should().BeFalse();
    }
}

