using Microsoft.Extensions.Hosting;

namespace NexArc.Authentication.Api.Services;

public sealed class SigningKeyProviderGuard : IHostedService
{
    private readonly IHostEnvironment _environment;
    private readonly ISigningKeyProvider _provider;

    public SigningKeyProviderGuard(IHostEnvironment environment, ISigningKeyProvider provider)
    {
        _environment = environment;
        _provider = provider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment() && _provider is EphemeralSigningKeyProvider)
        {
            throw new InvalidOperationException(
                "Ephemeral signing keys are not allowed outside Development. " +
                "Register a durable ISigningKeyProvider.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
