using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Provider.Auth0B2C.Extensions;
using NexArc.Authentication.Provider.Auth0B2C.Options;

namespace NexArc.Authentication.Provider.Auth0B2C.Tests;

public class Auth0B2COptionsTests
{
    [Fact]
    public void Defaults_Are_Applied()
    {
        var services = new ServiceCollection();
        services.AddProviderAuth0B2C(o =>
        {
            o.Authority = "https://example.auth0.com";
            o.ClientId = "test-client";
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<Auth0B2COptions>>().Value;

        Assert.Equal("auth0-b2c", options.ProviderKey);
        Assert.Equal("auth0-b2c", options.Scheme);
    }
}
