using NexArc.Authentication.DevBypass.Models;

namespace NexArc.Authentication.DevBypass.Services;

public interface IDevelopmentBypassUserProvider
{
    string ProviderKey { get; }
    bool Enabled { get; }
    IReadOnlyList<DevBypassUser> Users { get; }
}
