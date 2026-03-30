using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Abstractions.Models;
using NexArc.Authentication.Client.Extensions;
using NexArc.Authentication.Client.Options;

namespace NexArc.Authentication.Client.Tests;

public class HostApplicationBuilderExtensionsTests
{
    [Fact]
    public void Binds_Client_Options_Without_Configuring_Oidc_Token_Exchange()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["Auth:ProviderKey"] = "test-provider";
        builder.Configuration["Auth:ApiBaseUrl"] = "https://api.example.local";

        builder.AddClientAuthentication(builder.Configuration.GetRequiredSection("Auth"));

        builder.Services.AddAuthentication()
            .AddOpenIdConnect("custom-scheme", options =>
            {
                options.Authority = "https://login.example.local";
                options.ClientId = "client-id";
                options.ClientSecret = "client-secret";
            });

        using var services = builder.Services.BuildServiceProvider();
        var options = services.GetRequiredService<IOptions<ClientAuthenticationOptions>>().Value;
        var configurators = services.GetServices<IConfigureOptions<OpenIdConnectOptions>>();

        Assert.Equal("test-provider", options.ProviderKey);
        Assert.Equal("https://api.example.local", options.ApiBaseUrl);
        Assert.NotNull(services.GetRequiredService<IAuthorizationService>());
        Assert.DoesNotContain(configurators, configurator =>
            string.Equals(
                configurator.GetType().FullName,
                "NexArc.Authentication.Client.Services.OpenIdConnectTokenExchangeOptionsConfigurator",
                StringComparison.Ordinal));
    }

    [Fact]
    public void Configures_Oidc_Token_Exchange_From_Configuration()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["Auth:ProviderKey"] = "test-provider";
        builder.Configuration["Auth:ApiBaseUrl"] = "https://api.example.local";

        builder.AddOidcClientAuthentication(builder.Configuration.GetRequiredSection("Auth"));

        builder.Services.AddSingleton(new ProviderDescriptor("test-provider", "custom-scheme"));
        builder.Services.AddAuthentication()
            .AddOpenIdConnect("custom-scheme", options =>
            {
                options.Authority = "https://login.example.local";
                options.ClientId = "client-id";
                options.ClientSecret = "client-secret";
            });

        using var services = builder.Services.BuildServiceProvider();
        var options = services.GetRequiredService<IOptions<ClientAuthenticationOptions>>().Value;
        var monitor = services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>();
        var configurators = services.GetServices<IConfigureOptions<OpenIdConnectOptions>>();

        Assert.Equal("test-provider", options.ProviderKey);
        Assert.Equal("https://api.example.local", options.ApiBaseUrl);
        Assert.NotNull(services.GetRequiredService<IAuthorizationService>());
        Assert.Contains(configurators, configurator =>
            string.Equals(
                configurator.GetType().FullName,
                "NexArc.Authentication.Client.Services.OpenIdConnectTokenExchangeOptionsConfigurator",
                StringComparison.Ordinal));
        Assert.NotNull(monitor.Get("custom-scheme").Events.OnTokenValidated);
    }

    [Fact]
    public void Throws_When_Oidc_Token_Exchange_Provider_Key_Is_Missing()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["Auth:ApiBaseUrl"] = "https://api.example.local";

        var ex = Assert.Throws<InvalidOperationException>(() =>
            builder.AddOidcClientAuthentication(builder.Configuration.GetRequiredSection("Auth")));

        Assert.Equal("Configuration value 'Auth:ProviderKey' is required.", ex.Message);
    }

    [Fact]
    public void Throws_When_Api_Base_Url_Is_Not_Http_Or_Https()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["Auth:ProviderKey"] = "test-provider";
        builder.Configuration["Auth:ApiBaseUrl"] = "https+http://api";

        var ex = Assert.Throws<InvalidOperationException>(() =>
            builder.AddClientAuthentication(builder.Configuration.GetRequiredSection("Auth")));

        Assert.Equal("Configuration value 'Auth:ApiBaseUrl' must be set to an absolute http(s) URL.", ex.Message);
    }
}
