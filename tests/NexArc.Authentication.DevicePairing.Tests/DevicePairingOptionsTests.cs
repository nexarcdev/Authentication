using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexArc.Authentication.DevicePairing.Extensions;
using NexArc.Authentication.DevicePairing.Options;
using NexArc.Authentication.Utilities;

namespace NexArc.Authentication.DevicePairing.Tests;

public class DevicePairingOptionsTests
{
    [Fact]
    public void Defaults_Are_Applied()
    {
        var services = new ServiceCollection();
        services.AddProviderDevicePairing(_ => { });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<DevicePairingOptions>>().Value;

        Assert.Equal("device-pairing", options.ProviderKey);
        Assert.Equal("device-pairing", options.Scheme);
        Assert.Equal(8, options.CodeLength);
        Assert.Equal(300, options.CodeLifetimeSeconds);
        Assert.Equal(CodeAlphabet.Numeric, options.CodeAlphabet);
    }
}
