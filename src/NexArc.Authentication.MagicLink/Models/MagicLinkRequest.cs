namespace NexArc.Authentication.MagicLink.Models;

public sealed record MagicLinkRequest
{
    public string Destination { get; init; } = string.Empty;
}
