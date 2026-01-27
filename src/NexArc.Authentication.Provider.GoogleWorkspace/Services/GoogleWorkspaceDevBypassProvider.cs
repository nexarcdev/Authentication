using Microsoft.Extensions.Options;
using NexArc.Authentication.DevBypass.Models;
using NexArc.Authentication.DevBypass.Services;
using NexArc.Authentication.Provider.GoogleWorkspace.Options;

namespace NexArc.Authentication.Provider.GoogleWorkspace.Services;

public sealed class GoogleWorkspaceDevBypassProvider : IDevelopmentBypassUserProvider
{
    private readonly IOptions<GoogleWorkspaceOptions> _options;

    public GoogleWorkspaceDevBypassProvider(IOptions<GoogleWorkspaceOptions> options)
    {
        _options = options;
    }

    public string ProviderKey => _options.Value.ProviderKey;

    public bool Enabled => _options.Value.DevBypass.Enabled;

    public IReadOnlyList<DevBypassUser> Users => _options.Value.DevBypass.Users;
}
