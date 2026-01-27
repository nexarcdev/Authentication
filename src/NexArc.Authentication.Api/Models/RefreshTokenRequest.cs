namespace NexArc.Authentication.Api.Models;

public sealed record RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}
