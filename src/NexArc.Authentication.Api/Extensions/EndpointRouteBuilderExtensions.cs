using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NexArc.Authentication.Abstractions.Interfaces;
using NexArc.Authentication.Abstractions.Models;
using NexArc.Authentication.Api.Models;
using NexArc.Authentication.Api.Services;
using NexArc.Authentication.DevBypass.Services;
using Microsoft.Extensions.Hosting;

namespace NexArc.Authentication.Api.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAuthentication(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth");

        group.MapPost("/exchange/{providerKey}", async (
            string providerKey,
            TokenExchangeRequest? request,
            IServiceProvider services,
            CancellationToken ct) =>
        {
            if (request is null)
            {
                return Results.BadRequest("Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(providerKey))
            {
                return Results.BadRequest("Provider key is required.");
            }

            if (string.IsNullOrWhiteSpace(request.AccessToken) &&
                string.IsNullOrWhiteSpace(request.IdToken) &&
                string.IsNullOrWhiteSpace(request.AuthorizationCode) &&
                string.IsNullOrWhiteSpace(request.DevBypassUser))
            {
                return Results.BadRequest("At least one token or authorization code is required.");
            }

            var bypassProvider = services.GetServices<IDevelopmentBypassUserProvider>()
                .FirstOrDefault(p => string.Equals(p.ProviderKey, providerKey, StringComparison.OrdinalIgnoreCase)
                                     && p.Enabled);

            if (bypassProvider is not null && !string.IsNullOrWhiteSpace(request.DevBypassUser))
            {
                var env = services.GetRequiredService<IHostEnvironment>();
                if (!env.IsDevelopment())
                {
                    return Results.Unauthorized();
                }

                var user = bypassProvider.Users.FirstOrDefault(u =>
                    string.Equals(u.Subject, request.DevBypassUser, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(u.Email, request.DevBypassUser, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(u.Name, request.DevBypassUser, StringComparison.OrdinalIgnoreCase));

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                var subject = user.Subject ?? user.Email ?? user.Name;
                if (string.IsNullOrWhiteSpace(subject))
                {
                    return Results.Unauthorized();
                }

                var devTokenService = services.GetRequiredService<ITokenService>();
                var devTokenPair = await devTokenService.IssueAsync(new IssuedIdentity
                {
                    Subject = subject,
                    Email = user.Email,
                    Name = user.Name,
                    ProfileUrl = user.ProfileUrl,
                    Roles = user.Roles
                }, ct);

                return Results.Ok(devTokenPair);
            }

            var validator = services.GetKeyedService<IExternalIdentityValidator>(providerKey);
            if (validator is null)
            {
                return Results.NotFound("Provider not registered.");
            }

            ExternalIdentity externalIdentity;
            try
            {
                externalIdentity = await validator.ValidateAsync(new ExternalTokenContext
                {
                    ProviderKey = providerKey,
                    AccessToken = request.AccessToken,
                    IdToken = request.IdToken,
                    AuthorizationCode = request.AuthorizationCode
                }, ct);
            }
            catch (SecurityTokenException)
            {
                return Results.Unauthorized();
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(externalIdentity.Subject))
            {
                return Results.Unauthorized();
            }

            var profileProvider = services.GetKeyedService<IExternalProfileProvider>(providerKey);
            if (profileProvider is not null)
            {
                externalIdentity = await profileProvider.EnrichAsync(externalIdentity, ct);
            }

            var roles = externalIdentity.Roles;
            var membershipProvider = services.GetKeyedService<IExternalMembershipProvider>(providerKey);
            if (membershipProvider is not null)
            {
                roles = await membershipProvider.GetRolesAsync(externalIdentity, ct);
            }

            var normalizer = services.GetRequiredService<IIdentityNormalizer>();
            var issuedIdentity = normalizer.Normalize(externalIdentity, roles);

            var tokenService = services.GetRequiredService<ITokenService>();
            var tokenPair = await tokenService.IssueAsync(issuedIdentity, ct);

            return Results.Ok(tokenPair);
        });

        group.MapPost("/refresh", async (
            RefreshTokenRequest? request,
            ITokenService tokenService,
            CancellationToken ct) =>
        {
            if (request is null || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Results.BadRequest("Refresh token is required.");
            }

            var tokenPair = await tokenService.RefreshAsync(request.RefreshToken, ct);
            return tokenPair is null ? Results.Unauthorized() : Results.Ok(tokenPair);
        });

        var modules = endpoints.ServiceProvider.GetServices<IApiEndpointModule>();
        foreach (var module in modules)
        {
            module.MapEndpoints(endpoints);
        }

        return endpoints;
    }
}
