using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Abstractions.Models;
using NexArc.Authentication.Client.Authentication;
using NexArc.Authentication.Client.Options;

namespace NexArc.Authentication.Client.Services;

internal sealed class ClientAuthenticationDefaultsConfigurator : IConfigureOptions<AuthenticationOptions>
{
    private readonly IOptions<ClientAuthenticationOptions> _clientOptions;
    private readonly IEnumerable<ProviderDescriptor> _providers;

    public ClientAuthenticationDefaultsConfigurator(
        IOptions<ClientAuthenticationOptions> clientOptions,
        IEnumerable<ProviderDescriptor> providers)
    {
        _clientOptions = clientOptions;
        _providers = providers;
    }

    public void Configure(AuthenticationOptions options)
    {
        var providerKey = _clientOptions.Value.ProviderKey;
        var scheme = _providers.FirstOrDefault(p => p.ProviderKey == providerKey)?.Scheme ?? providerKey;

        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = TokenStoreAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = scheme;
    }
}
