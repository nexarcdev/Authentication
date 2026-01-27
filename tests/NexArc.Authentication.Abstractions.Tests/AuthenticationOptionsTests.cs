using NexArc.Authentication.Abstractions.Options;

namespace NexArc.Authentication.Abstractions.Tests;

public class AuthenticationOptionsTests
{
    [Fact]
    public void Defaults_Are_Sane()
    {
        var options = new AuthenticationOptions();

        Assert.Equal(TimeSpan.FromMinutes(10), options.AccessTokenLifetime);
        Assert.True(options.RefreshTokensEnabled);
        Assert.Equal(TimeSpan.FromDays(14), options.RefreshTokenLifetime);
    }
}
