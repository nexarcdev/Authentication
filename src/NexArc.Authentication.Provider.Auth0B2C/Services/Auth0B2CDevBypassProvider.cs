using Microsoft.Extensions.Options;
using NexArc.Authentication.DevBypass.Models;
using NexArc.Authentication.DevBypass.Services;
using NexArc.Authentication.Provider.Auth0B2C.Options;

namespace NexArc.Authentication.Provider.Auth0B2C.Services;

public sealed class Auth0B2CDevBypassProvider : IDevelopmentBypassUserProvider
{
    private readonly IOptions<Auth0B2COptions> _options;

    public Auth0B2CDevBypassProvider(IOptions<Auth0B2COptions> options)
    {
        _options = options;
    }

    public string ProviderKey => _options.Value.ProviderKey;

    public bool Enabled => _options.Value.DevBypass.Enabled;

    public IReadOnlyList<DevBypassUser> Users => _options.Value.DevBypass.Users;
}
