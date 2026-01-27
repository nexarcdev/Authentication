using NexArc.Authentication.Abstractions.Claims;

namespace NexArc.Authentication.Abstractions.Tests;

public class CanonicalClaimTypesTests
{
    [Fact]
    public void Uses_Standard_Claim_Names()
    {
        Assert.Equal("sub", CanonicalClaimTypes.Subject);
        Assert.Equal("email", CanonicalClaimTypes.Email);
        Assert.Equal("name", CanonicalClaimTypes.Name);
        Assert.Equal("picture", CanonicalClaimTypes.ProfileUrl);
        Assert.Equal("role", CanonicalClaimTypes.Role);
        Assert.Equal("sid", CanonicalClaimTypes.SessionId);
    }
}
