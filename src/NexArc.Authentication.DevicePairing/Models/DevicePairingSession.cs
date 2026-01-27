namespace NexArc.Authentication.DevicePairing.Models;

public sealed record DevicePairingSession
{
    public string SessionId { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? DeviceId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
}
