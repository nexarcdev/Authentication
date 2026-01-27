using NexArc.Authentication.Abstractions.Options;
using NexArc.Authentication.MagicLink.DevBypass;
using NexArc.Authentication.Utilities;

namespace NexArc.Authentication.MagicLink.Options;

public sealed class MagicLinkOptions : ProviderOptions
{
    public int CodeLength { get; set; } = 8;
    public CodeAlphabet CodeAlphabet { get; set; } = CodeAlphabet.Unambiguous;
    public int CodeLifetimeSeconds { get; set; } = 600;
    public string? RedeemUrl { get; set; }
    public MagicLinkDevBypassOptions DevBypass { get; set; } = new();
}
