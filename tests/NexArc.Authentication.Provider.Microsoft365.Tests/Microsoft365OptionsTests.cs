using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Provider.Microsoft365.Extensions;
using NexArc.Authentication.Provider.Microsoft365.Options;

namespace NexArc.Authentication.Provider.Microsoft365.Tests;

public class Microsoft365OptionsTests
{
    [Fact]
    public void Defaults_Are_Applied()
    {
        var services = new ServiceCollection();
        services.AddProviderMicrosoft365(o =>
        {
            o.Authority = "https://login.microsoftonline.com/tenant/v2.0";
            o.ClientId = "test-client";
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<Microsoft365Options>>().Value;

        Assert.Equal("microsoft-365", options.ProviderKey);
        Assert.Equal("microsoft-365", options.Scheme);
    }
}
