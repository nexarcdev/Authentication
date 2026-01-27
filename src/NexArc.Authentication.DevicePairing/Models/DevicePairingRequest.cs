namespace NexArc.Authentication.DevicePairing.Models;

public sealed record DevicePairingRequest
{
    public string? DeviceId { get; init; }
}
