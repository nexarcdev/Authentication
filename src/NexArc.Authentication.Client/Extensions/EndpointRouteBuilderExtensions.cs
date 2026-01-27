using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NexArc.Authentication.Client.Services;

namespace NexArc.Authentication.Client.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapClientAuthentication(this IEndpointRouteBuilder endpoints)
    {
        var modules = endpoints.ServiceProvider.GetServices<IClientEndpointModule>();
        foreach (var module in modules)
        {
            module.MapEndpoints(endpoints);
        }

        return endpoints;
    }
}
