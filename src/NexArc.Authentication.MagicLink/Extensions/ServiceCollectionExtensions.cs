using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NexArc.Authentication.Api.Services;
using NexArc.Authentication.Client.Services;
using NexArc.Authentication.MagicLink.Endpoints;
using NexArc.Authentication.MagicLink.Options;
using NexArc.Authentication.Utilities;

namespace NexArc.Authentication.MagicLink.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProviderMagicLink(
        this IServiceCollection services,
        IConfigurationSection section)
    {
        return services.AddProviderMagicLink(options => section.Bind(options));
    }

    public static IServiceCollection AddProviderMagicLink(
        this IServiceCollection services,
        Action<MagicLinkOptions> configure)
    {
        var local = new MagicLinkOptions();
        configure(local);
        ApplyDefaults(local);

        services.AddOptions<MagicLinkOptions>()
            .Configure(options =>
            {
                configure(options);
                ApplyDefaults(options);
            })
            .Validate(o => !string.IsNullOrWhiteSpace(o.ProviderKey), "ProviderKey is required.")
            .Validate(o => o.CodeLength > 0, "CodeLength must be positive.")
            .Validate(o => o.CodeLifetimeSeconds > 0, "CodeLifetimeSeconds must be positive.")
            .ValidateOnStart();

        services.TryAddSingleton<ISecureCodeGenerator, SecureCodeGenerator>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IApiEndpointModule, MagicLinkApiEndpoints>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IClientEndpointModule, MagicLinkClientEndpoints>());

        return services;
    }

    private static void ApplyDefaults(MagicLinkOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ProviderKey))
        {
            options.ProviderKey = "magic-link";
        }

        if (string.IsNullOrWhiteSpace(options.Scheme))
        {
            options.Scheme = options.ProviderKey;
        }

        if (options.CodeLength <= 0)
        {
            options.CodeLength = 8;
        }

        if (options.CodeLifetimeSeconds <= 0)
        {
            options.CodeLifetimeSeconds = 600;
        }
    }
}
