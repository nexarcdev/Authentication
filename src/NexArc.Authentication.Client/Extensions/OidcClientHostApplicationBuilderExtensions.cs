using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace NexArc.Authentication.Client.Extensions;

public static class OidcClientHostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddOidcClientAuthentication(
        this IHostApplicationBuilder builder,
        IConfigurationSection authSection)
    {
        builder.Services.AddOidcClientAuthentication(authSection);
        return builder;
    }
}
