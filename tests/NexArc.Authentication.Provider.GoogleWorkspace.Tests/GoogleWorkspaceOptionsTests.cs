using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Provider.GoogleWorkspace.Extensions;
using NexArc.Authentication.Provider.GoogleWorkspace.Options;

namespace NexArc.Authentication.Provider.GoogleWorkspace.Tests;

public class GoogleWorkspaceOptionsTests
{
    [Fact]
    public void Defaults_Are_Applied()
    {
        var services = new ServiceCollection();
        services.AddProviderGoogleWorkspace(o => { o.ClientId = "test-client"; });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<GoogleWorkspaceOptions>>().Value;

        Assert.Equal("google-workspace", options.ProviderKey);
        Assert.Equal("google-workspace", options.Scheme);
    }
}
