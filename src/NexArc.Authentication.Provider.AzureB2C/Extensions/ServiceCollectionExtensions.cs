using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexArc.Authentication.Abstractions.Interfaces;
using NexArc.Authentication.Abstractions.Models;
using NexArc.Authentication.DevBypass.Services;
using NexArc.Authentication.Provider.AzureB2C.Options;
using NexArc.Authentication.Provider.AzureB2C.Services;

namespace NexArc.Authentication.Provider.AzureB2C.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProviderAzureB2C(
        this IServiceCollection services,
        IConfigurationSection section)
    {
        return services.AddProviderAzureB2C(options => section.Bind(options));
    }

    public static IServiceCollection AddProviderAzureB2C(
        this IServiceCollection services,
        Action<AzureB2COptions> configure)
    {
        var local = new AzureB2COptions();
        configure(local);
        ApplyDefaults(local);

        services.AddOptions<AzureB2COptions>()
            .Configure(options =>
            {
                configure(options);
                ApplyDefaults(options);
            })
            .Validate(o => !string.IsNullOrWhiteSpace(o.ProviderKey), "ProviderKey is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Authority), "Authority is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.ClientId), "ClientId is required.")
            .ValidateOnStart();

        services.AddKeyedScoped<IExternalIdentityValidator>(
            local.ProviderKey,
            (sp, _) => new AzureB2CTokenValidator(
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AzureB2COptions>>()));

        services.AddSingleton(new ProviderDescriptor(local.ProviderKey, local.Scheme));
        services.AddSingleton<IDevelopmentBypassUserProvider, AzureB2CDevBypassProvider>();

        services.AddAuthentication()
            .AddOpenIdConnect(local.Scheme, options =>
            {
                options.Authority = local.Authority;
                options.ClientId = local.ClientId;
                options.ClientSecret = local.ClientSecret;
                options.ResponseType = "code";
                options.UsePkce = true;
                options.SaveTokens = true;
            });

        return services;
    }

    private static void ApplyDefaults(AzureB2COptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ProviderKey))
        {
            options.ProviderKey = "azure-b2c";
        }

        if (string.IsNullOrWhiteSpace(options.Scheme))
        {
            options.Scheme = options.ProviderKey;
        }
    }
}
