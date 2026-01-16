using Planner.Contracts.API.Auth;

namespace Planner.BlazorApp.FormModels;

/// <summary>
/// UI form model for the login screen.
/// </summary>
public sealed class LoginFormModel {
    public string? Email { get; set; } = string.Empty;
    public string? Password { get; set; } = string.Empty;
}

public static class LoginFormMapper {
    /// <summary>
    /// Project the UI model into the shared login request contract.
    /// </summary>
    public static LoginRequest ToRequestDto(this LoginFormModel model) {
        return new LoginRequest(
            model.Email ?? string.Empty,
            model.Password ?? string.Empty);
    }
}
