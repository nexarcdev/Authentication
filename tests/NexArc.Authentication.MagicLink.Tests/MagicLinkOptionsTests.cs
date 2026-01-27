using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexArc.Authentication.MagicLink.Extensions;
using NexArc.Authentication.MagicLink.Options;
using NexArc.Authentication.Utilities;

namespace NexArc.Authentication.MagicLink.Tests;

public class MagicLinkOptionsTests
{
    [Fact]
    public void Defaults_Are_Applied()
    {
        var services = new ServiceCollection();
        services.AddProviderMagicLink(_ => { });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<MagicLinkOptions>>().Value;

        Assert.Equal("magic-link", options.ProviderKey);
        Assert.Equal("magic-link", options.Scheme);
        Assert.Equal(8, options.CodeLength);
        Assert.Equal(600, options.CodeLifetimeSeconds);
        Assert.Equal(CodeAlphabet.Unambiguous, options.CodeAlphabet);
    }
}
