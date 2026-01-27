namespace NexArc.Authentication.DevicePairing.Models;

public sealed record DevicePairingIssuedResponse
{
    public string SessionId { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? PairingUrl { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
}
