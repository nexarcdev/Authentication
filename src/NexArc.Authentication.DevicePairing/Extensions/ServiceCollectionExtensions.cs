using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NexArc.Authentication.Api.Services;
using NexArc.Authentication.Client.Services;
using NexArc.Authentication.DevicePairing.Endpoints;
using NexArc.Authentication.DevicePairing.Options;
using NexArc.Authentication.Utilities;

namespace NexArc.Authentication.DevicePairing.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProviderDevicePairing(
        this IServiceCollection services,
        IConfigurationSection section)
    {
        return services.AddProviderDevicePairing(options => section.Bind(options));
    }

    public static IServiceCollection AddProviderDevicePairing(
        this IServiceCollection services,
        Action<DevicePairingOptions> configure)
    {
        var local = new DevicePairingOptions();
        configure(local);
        ApplyDefaults(local);

        services.AddOptions<DevicePairingOptions>()
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
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IApiEndpointModule, DevicePairingApiEndpoints>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IClientEndpointModule, DevicePairingClientEndpoints>());

        return services;
    }

    private static void ApplyDefaults(DevicePairingOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ProviderKey))
        {
            options.ProviderKey = "device-pairing";
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
            options.CodeLifetimeSeconds = 300;
        }
    }
}
