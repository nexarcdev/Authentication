using NexArc.Authentication.DevicePairing.Models;

namespace NexArc.Authentication.DevicePairing.Services;

public interface IDevicePairingVerifier
{
    Task<DevicePairingApproval> ApproveAsync(DevicePairingSession session, CancellationToken ct);
}
