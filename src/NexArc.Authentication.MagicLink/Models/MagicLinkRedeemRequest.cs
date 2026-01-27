namespace NexArc.Authentication.MagicLink.Models;

public sealed record MagicLinkRedeemRequest
{
    public string Code { get; init; } = string.Empty;
}
