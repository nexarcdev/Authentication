using NexArc.Authentication.DevBypass.Models;

namespace NexArc.Authentication.MagicLink.DevBypass;

public sealed record MagicLinkDevBypassDestination
{
    public string Destination { get; init; } = string.Empty;
    public DevBypassUser User { get; init; } = new();
}
