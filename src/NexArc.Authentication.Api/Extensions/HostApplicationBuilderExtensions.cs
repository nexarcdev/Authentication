using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NexArc.Authentication.Api.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddApiAuthentication(
        this IHostApplicationBuilder builder,
        IConfigurationSection authSection)
    {
        builder.Services.AddApiAuthentication(authSection);
        return builder;
    }
}
