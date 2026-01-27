using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NexArc.Authentication.DevBypass.Services;

namespace NexArc.Authentication.DevBypass.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDevelopmentBypassGuard(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, DevelopmentBypassGuard>());
        return services;
    }
}
