using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexArc.Authentication.Abstractions.Interfaces;
using NexArc.Authentication.Abstractions.Models;
using NexArc.Authentication.DevBypass.Services;
using NexArc.Authentication.Provider.GoogleWorkspace.Options;
using NexArc.Authentication.Provider.GoogleWorkspace.Services;

namespace NexArc.Authentication.Provider.GoogleWorkspace.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProviderGoogleWorkspace(
        this IServiceCollection services,
        IConfigurationSection section)
    {
        return services.AddProviderGoogleWorkspace(options => section.Bind(options));
    }

    public static IServiceCollection AddProviderGoogleWorkspace(
        this IServiceCollection services,
        Action<GoogleWorkspaceOptions> configure)
    {
        var local = new GoogleWorkspaceOptions();
        configure(local);
        ApplyDefaults(local);

        services.AddOptions<GoogleWorkspaceOptions>()
            .Configure(options =>
            {
                configure(options);
                ApplyDefaults(options);
            })
            .Validate(o => !string.IsNullOrWhiteSpace(o.ProviderKey), "ProviderKey is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.ClientId), "ClientId is required.")
            .ValidateOnStart();

        services.AddKeyedScoped<IExternalIdentityValidator>(
            local.ProviderKey,
            (sp, _) => new GoogleWorkspaceTokenValidator(
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GoogleWorkspaceOptions>>()));

        services.AddSingleton(new ProviderDescriptor(local.ProviderKey, local.Scheme));
        services.AddSingleton<IDevelopmentBypassUserProvider, GoogleWorkspaceDevBypassProvider>();

        services.AddAuthentication()
            .AddOpenIdConnect(local.Scheme, options =>
            {
                options.Authority = local.Authority;
                options.ClientId = local.ClientId;
                options.ClientSecret = local.ClientSecret;
                options.ResponseType = "code";
                options.UsePkce = true;
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.Scope.Add("email");
                options.Scope.Add("profile");
            });

        return services;
    }

    private static void ApplyDefaults(GoogleWorkspaceOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ProviderKey))
        {
            options.ProviderKey = "google-workspace";
        }

        if (string.IsNullOrWhiteSpace(options.Scheme))
        {
            options.Scheme = options.ProviderKey;
        }
    }
}
