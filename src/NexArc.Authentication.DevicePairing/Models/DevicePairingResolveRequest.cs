namespace NexArc.Authentication.DevicePairing.Models;

public sealed record DevicePairingResolveRequest
{
    public string Code { get; init; } = string.Empty;
}
