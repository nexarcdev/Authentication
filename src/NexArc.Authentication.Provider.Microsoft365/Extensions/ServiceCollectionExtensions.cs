using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexArc.Authentication.Abstractions.Interfaces;
using NexArc.Authentication.Abstractions.Models;
using NexArc.Authentication.DevBypass.Services;
using NexArc.Authentication.Provider.Microsoft365.Options;
using NexArc.Authentication.Provider.Microsoft365.Services;

namespace NexArc.Authentication.Provider.Microsoft365.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProviderMicrosoft365(
        this IServiceCollection services,
        IConfigurationSection section)
    {
        return services.AddProviderMicrosoft365(options => section.Bind(options));
    }

    public static IServiceCollection AddProviderMicrosoft365(
        this IServiceCollection services,
        Action<Microsoft365Options> configure)
    {
        var local = new Microsoft365Options();
        configure(local);
        ApplyDefaults(local);

        services.AddOptions<Microsoft365Options>()
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
            (sp, _) => new Microsoft365TokenValidator(
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft365Options>>()));

        services.AddSingleton(new ProviderDescriptor(local.ProviderKey, local.Scheme));
        services.AddSingleton<IDevelopmentBypassUserProvider, Microsoft365DevBypassProvider>();

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

    private static void ApplyDefaults(Microsoft365Options options)
    {
        if (string.IsNullOrWhiteSpace(options.ProviderKey))
        {
            options.ProviderKey = "microsoft-365";
        }

        if (string.IsNullOrWhiteSpace(options.Scheme))
        {
            options.Scheme = options.ProviderKey;
        }
    }
}
