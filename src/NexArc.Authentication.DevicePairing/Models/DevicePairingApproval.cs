using NexArc.Authentication.Abstractions.Models;

namespace NexArc.Authentication.DevicePairing.Models;

public sealed record DevicePairingApproval
{
    public IssuedIdentity Identity { get; init; } = new();
}
