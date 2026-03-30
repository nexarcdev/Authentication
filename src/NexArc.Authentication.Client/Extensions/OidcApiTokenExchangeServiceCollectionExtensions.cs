using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Abstractions.Models;
using NexArc.Authentication.Client.Options;
using NexArc.Authentication.Client.Services;

namespace NexArc.Authentication.Client.Extensions;

public static class OidcApiTokenExchangeServiceCollectionExtensions
{
    public static IServiceCollection AddApiTokenExchangeOnOidcSignIn(
        this IServiceCollection services,
        string providerKey)
    {
        services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>>(sp =>
            new OpenIdConnectTokenExchangeOptionsConfigurator(
                providerKey,
                null,
                sp.GetServices<ProviderDescriptor>(),
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<IOptions<ClientAuthenticationOptions>>(),
                sp.GetRequiredService<ITokenStore>(),
                sp.GetRequiredService<ILoggerFactory>()));

        return services;
    }

    public static IServiceCollection AddApiTokenExchangeOnOidcSignIn(
        this IServiceCollection services,
        string scheme,
        string providerKey)
    {
        services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>>(sp =>
            new OpenIdConnectTokenExchangeOptionsConfigurator(
                providerKey,
                scheme,
                sp.GetServices<ProviderDescriptor>(),
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<IOptions<ClientAuthenticationOptions>>(),
                sp.GetRequiredService<ITokenStore>(),
                sp.GetRequiredService<ILoggerFactory>()));

        return services;
    }
}
