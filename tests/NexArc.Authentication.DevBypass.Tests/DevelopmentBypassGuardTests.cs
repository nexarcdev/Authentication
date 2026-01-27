using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using NexArc.Authentication.DevBypass.Services;

namespace NexArc.Authentication.DevBypass.Tests;

public class DevelopmentBypassGuardTests
{
    [Fact]
    public async Task Throws_When_Enabled_In_Production()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Providers:GoogleWorkspace:DevBypass:Enabled"] = "true"
            })
            .Build();

        var guard = new DevelopmentBypassGuard(new TestHostEnvironment("Production"), config);

        await Assert.ThrowsAsync<InvalidOperationException>(() => guard.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Does_Not_Throw_In_Development()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Providers:GoogleWorkspace:DevBypass:Enabled"] = "true"
            })
            .Build();

        var guard = new DevelopmentBypassGuard(new TestHostEnvironment("Development"), config);

        await guard.StartAsync(CancellationToken.None);
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
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
