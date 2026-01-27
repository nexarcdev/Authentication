using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Provider.AzureB2C.Extensions;
using NexArc.Authentication.Provider.AzureB2C.Options;

namespace NexArc.Authentication.Provider.AzureB2C.Tests;

public class AzureB2COptionsTests
{
    [Fact]
    public void Defaults_Are_Applied()
    {
        var services = new ServiceCollection();
        services.AddProviderAzureB2C(o =>
        {
            o.Authority = "https://tenant.b2clogin.com/tenant.onmicrosoft.com/policy";
            o.ClientId = "test-client";
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AzureB2COptions>>().Value;

        Assert.Equal("azure-b2c", options.ProviderKey);
        Assert.Equal("azure-b2c", options.Scheme);
    }
}
