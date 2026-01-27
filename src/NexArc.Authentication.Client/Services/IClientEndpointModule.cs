using Microsoft.AspNetCore.Routing;

namespace NexArc.Authentication.Client.Services;

public interface IClientEndpointModule
{
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
