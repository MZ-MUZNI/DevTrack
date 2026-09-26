using System.ComponentModel.DataAnnotations;

namespace DevTrack.Api.Contracts;

public sealed class RegisterRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6)]
    public string Password { get; init; } = string.Empty;

    [StringLength(100)]
    public string? DisplayName { get; init; }
}

public sealed class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed record TokenResponse(string AccessToken, DateTime ExpiresAtUtc);
