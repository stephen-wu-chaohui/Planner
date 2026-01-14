namespace Planner.Contracts.API.Auth;

/// <summary>
/// Request payload for user login.
/// </summary>
/// <param name="Email">User's email address.</param>
/// <param name="Password">Plain-text password supplied by the user.</param>
public sealed record LoginRequest(
    string Email,
    string Password);

/// <summary>
/// Response payload containing a JWT access token.
/// </summary>
/// <param name="AccessToken">JWT token for authenticated API calls.</param>
public sealed record LoginResponse(
    string AccessToken);
