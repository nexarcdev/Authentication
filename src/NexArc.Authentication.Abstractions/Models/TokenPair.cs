namespace NexArc.Authentication.Abstractions.Models;

public sealed record TokenPair
{
    public string AccessToken { get; init; } = string.Empty;
    public DateTimeOffset AccessTokenExpiresAt { get; init; }
    public string? RefreshToken { get; init; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; init; }
}
