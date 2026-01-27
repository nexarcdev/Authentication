using Microsoft.Extensions.Options;
using NexArc.Authentication.DevBypass.Models;
using NexArc.Authentication.DevBypass.Services;
using NexArc.Authentication.Provider.Microsoft365.Options;

namespace NexArc.Authentication.Provider.Microsoft365.Services;

public sealed class Microsoft365DevBypassProvider : IDevelopmentBypassUserProvider
{
    private readonly IOptions<Microsoft365Options> _options;

    public Microsoft365DevBypassProvider(IOptions<Microsoft365Options> options)
    {
        _options = options;
    }

    public string ProviderKey => _options.Value.ProviderKey;

    public bool Enabled => _options.Value.DevBypass.Enabled;

    public IReadOnlyList<DevBypassUser> Users => _options.Value.DevBypass.Users;
}
