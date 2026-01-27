using Microsoft.AspNetCore.Routing;

namespace NexArc.Authentication.Api.Services;

public interface IApiEndpointModule
{
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
