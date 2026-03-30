using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Client.Authentication;
using NexArc.Authentication.Client.Options;
using NexArc.Authentication.Client.Services;
using NexArc.Authentication.Client.Endpoints;
using NexArc.Authentication.DevBypass.Extensions;

namespace NexArc.Authentication.Client.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClientAuthentication(
        this IServiceCollection services,
        Action<ClientAuthenticationOptions> configure)
    {
        var localOptions = new ClientAuthenticationOptions();
        configure(localOptions);

        services.AddOptions<ClientAuthenticationOptions>()
            .Configure(configure)
            .Validate(options => !string.IsNullOrWhiteSpace(options.ProviderKey), "ProviderKey is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ApiBaseUrl), "ApiBaseUrl is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ApiClientName), "ApiClientName is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.AuthApiClientName), "AuthApiClientName is required.")
            .Validate(options => options.RefreshBeforeExpiry >= TimeSpan.Zero, "RefreshBeforeExpiry must be zero or greater.")
            .ValidateOnStart();

        services.TryAddSingleton<ITokenStore, InMemoryTokenStore>();
        services.TryAddSingleton<IApiAccessTokenProvider, TokenStoreAccessTokenProvider>();
        services.TryAddTransient<ApiBearerTokenHandler>();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IClientEndpointModule, ClientAuthEndpoints>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IClientEndpointModule, DevBypassClientEndpoints>());

        services.AddAuthentication()
            .AddCookie()
            .AddScheme<AuthenticationSchemeOptions, TokenStoreAuthenticationHandler>(
                TokenStoreAuthenticationDefaults.AuthenticationScheme,
                _ => { });
        services.AddSingleton<IConfigureOptions<AuthenticationOptions>, ClientAuthenticationDefaultsConfigurator>();

        services.AddHttpClient(localOptions.ApiClientName, (sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<ClientAuthenticationOptions>>().Value;
                client.BaseAddress = new Uri(options.ApiBaseUrl);
            })
            .AddHttpMessageHandler<ApiBearerTokenHandler>();

        services.AddHttpClient(localOptions.AuthApiClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ClientAuthenticationOptions>>().Value;
            client.BaseAddress = new Uri(options.ApiBaseUrl);
        });

        services.AddDevelopmentBypassGuard();

        return services;
    }

    public static IServiceCollection AddClientAuthentication(
        this IServiceCollection services,
        IConfigurationSection authSection)
    {
        var options = new ClientAuthenticationOptions();
        authSection.Bind(options);

        if (string.IsNullOrWhiteSpace(options.ProviderKey))
            throw new InvalidOperationException("Configuration value 'Auth:ProviderKey' is required.");

        if (!IsHttpUrl(options.ApiBaseUrl))
            throw new InvalidOperationException("Configuration value 'Auth:ApiBaseUrl' must be set to an absolute http(s) URL.");

        services.AddClientAuthentication(configured => authSection.Bind(configured));
        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddOidcClientAuthentication(
        this IServiceCollection services,
        IConfigurationSection authSection)
    {
        services.AddClientAuthentication(authSection);

        var providerKey = authSection["ProviderKey"];
        if (string.IsNullOrWhiteSpace(providerKey))
            throw new InvalidOperationException("Configuration value 'Auth:ProviderKey' is required.");

        services.AddApiTokenExchangeOnOidcSignIn(providerKey);
        return services;
    }

    private static bool IsHttpUrl(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && (value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("http://", StringComparison.OrdinalIgnoreCase));
}
