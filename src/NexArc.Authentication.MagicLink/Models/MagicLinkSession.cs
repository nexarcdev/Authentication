namespace NexArc.Authentication.MagicLink.Models;

public sealed record MagicLinkSession
{
    public string SessionId { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Destination { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
