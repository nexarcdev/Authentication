using NexArc.Authentication.Abstractions.Models;
using NexArc.Authentication.Api.Services;

namespace NexArc.Authentication.Api.Tests;

public class DefaultIdentityNormalizerTests
{
    [Fact]
    public void Normalizes_Identity_With_Roles()
    {
        var normalizer = new DefaultIdentityNormalizer();
        var external = new ExternalIdentity
        {
            Subject = "user-1",
            Email = "user@example.com",
            Name = "User",
            ProfileUrl = "https://example.com/p.png",
            Claims = new Dictionary<string, string?> { ["custom"] = "value" }
        };

        var roles = new[] { "Admin" };
        var issued = normalizer.Normalize(external, roles);

        Assert.Equal("user-1", issued.Subject);
        Assert.Equal("user@example.com", issued.Email);
        Assert.Equal("User", issued.Name);
        Assert.Equal("https://example.com/p.png", issued.ProfileUrl);
        Assert.Equal(roles, issued.Roles);
        Assert.Equal("value", issued.Claims["custom"]);
    }
}
