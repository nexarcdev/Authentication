using NexArc.Authentication.DevBypass.Models;

namespace NexArc.Authentication.DevicePairing.DevBypass;

public sealed record DevicePairingDevBypassDevice
{
    public string DeviceId { get; init; } = string.Empty;
    public DevBypassUser User { get; init; } = new();
}
