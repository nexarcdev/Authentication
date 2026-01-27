using NexArc.Authentication.DevicePairing.Models;

namespace NexArc.Authentication.DevicePairing.Services;

public interface IDevicePairingSessionStore
{
    Task SaveAsync(DevicePairingSession session, CancellationToken ct);
    Task<DevicePairingSession?> FindByCodeAsync(string code, CancellationToken ct);
    Task CompleteAsync(string sessionId, CancellationToken ct);
}
