using NexArc.Authentication.Abstractions.Options;
using NexArc.Authentication.DevicePairing.DevBypass;
using NexArc.Authentication.Utilities;

namespace NexArc.Authentication.DevicePairing.Options;

public sealed class DevicePairingOptions : ProviderOptions
{
    public int CodeLength { get; set; } = 8;
    public CodeAlphabet CodeAlphabet { get; set; } = CodeAlphabet.Numeric;
    public int CodeLifetimeSeconds { get; set; } = 300;
    public string? PairingUrl { get; set; }
    public DevicePairingDevBypassOptions DevBypass { get; set; } = new();
}
