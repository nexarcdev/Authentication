using Microsoft.Extensions.Options;
using NexArc.Authentication.DevBypass.Models;
using NexArc.Authentication.DevBypass.Services;
using NexArc.Authentication.Provider.AzureB2C.Options;

namespace NexArc.Authentication.Provider.AzureB2C.Services;

public sealed class AzureB2CDevBypassProvider : IDevelopmentBypassUserProvider
{
    private readonly IOptions<AzureB2COptions> _options;

    public AzureB2CDevBypassProvider(IOptions<AzureB2COptions> options)
    {
        _options = options;
    }

    public string ProviderKey => _options.Value.ProviderKey;

    public bool Enabled => _options.Value.DevBypass.Enabled;

    public IReadOnlyList<DevBypassUser> Users => _options.Value.DevBypass.Users;
}
