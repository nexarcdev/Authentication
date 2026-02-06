using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Client.Extensions;

namespace NexArc.Authentication.Client.Tests;

public class OidcApiTokenExchangeServiceCollectionExtensionsTests
{
    [Fact]
    public void Registers_OnTokenValidated_Handler_For_Configured_Scheme()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddClientAuthentication(options =>
        {
            options.ProviderKey = "test-provider";
            options.ApiBaseUrl = "https://api.example.local";
        });

        services.AddAuthentication()
            .AddOpenIdConnect("test-scheme", options =>
            {
                options.Authority = "https://login.microsoftonline.com/common/v2.0";
                options.ClientId = "test-client";
                options.ClientSecret = "test-secret";
            });

        services.AddApiTokenExchangeOnOidcSignIn("test-scheme", "test-provider");

        using var provider = services.BuildServiceProvider();
        var monitor = provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>();
        var options = monitor.Get("test-scheme");

        Assert.NotNull(options.Events.OnTokenValidated);
    }
}
