using NexArc.Authentication.Abstractions.Models;

namespace NexArc.Authentication.MagicLink.Models;

public sealed record MagicLinkApproval
{
    public IssuedIdentity Identity { get; init; } = new();
}
