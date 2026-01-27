using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using NexArc.Authentication.Api.Services;

namespace NexArc.Authentication.Api.Tests;

public class SigningKeyProviderGuardTests
{
    [Fact]
    public async Task Throws_In_NonDevelopment_When_Using_Ephemeral_Key()
    {
        var guard = new SigningKeyProviderGuard(
            new TestHostEnvironment("Production"),
            new EphemeralSigningKeyProvider());

        await Assert.ThrowsAsync<InvalidOperationException>(() => guard.StartAsync(CancellationToken.None));
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "TestApp";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(AppContext.BaseDirectory);
    }
}
