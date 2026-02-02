using System.ComponentModel.DataAnnotations;

namespace Planner.Infrastructure.Persistence.Auth;

public record JwtOptions {
    public const string SectionName = "JwtOptions";

    [Required(AllowEmptyStrings = false)]
    [MinLength(32, ErrorMessage = "Secret must be at least 32 characters.")]
    public string Secret { get; init; } = string.Empty;

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Range(1, 1440)] // Between 1 minute and 1 day
    public int ExpirationInMinutes { get; init; }

    [Required]
    public string Audience { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [MinLength(32, ErrorMessage = "SigningKey must be at least 32 characters.")]
    public string SigningKey { get; init; } = string.Empty;
}

