namespace NexArc.Authentication.MagicLink.Models;

public sealed record MagicLinkIssuedResponse
{
    public string SessionId { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? RedeemUrl { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
}
