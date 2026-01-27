namespace NexArc.Authentication.DevicePairing.DevBypass;

public sealed class DevicePairingDevBypassOptions
{
    public bool Enabled { get; set; }
    public List<DevicePairingDevBypassDevice> Devices { get; set; } = new();
}
