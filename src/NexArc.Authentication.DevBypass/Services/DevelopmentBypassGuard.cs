using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace NexArc.Authentication.DevBypass.Services;

public sealed class DevelopmentBypassGuard : IHostedService
{
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public DevelopmentBypassGuard(IHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_environment.IsDevelopment())
        {
            return Task.CompletedTask;
        }

        var providersSection = _configuration.GetSection("Auth:Providers");
        foreach (var provider in providersSection.GetChildren())
        {
            var enabled = provider.GetSection("DevBypass").GetValue<bool>("Enabled");
            if (enabled)
            {
                throw new InvalidOperationException(
                    $"Development bypass is enabled for provider '{provider.Key}' outside Development.");
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
