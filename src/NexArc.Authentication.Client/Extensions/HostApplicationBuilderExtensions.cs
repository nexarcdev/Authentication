using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace NexArc.Authentication.Client.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddClientAuthentication(
        this IHostApplicationBuilder builder,
        IConfigurationSection authSection)
    {
        builder.Services.AddClientAuthentication(authSection);
        return builder;
    }
}
