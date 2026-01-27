using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NexArc.Authentication.Abstractions.Options;
using NexArc.Authentication.Api.Services;
using NexArc.Authentication.DevBypass.Extensions;

namespace NexArc.Authentication.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppAuthentication(
        this IServiceCollection services,
        Action<AuthenticationOptions> configure)
    {
        services.AddOptions<AuthenticationOptions>()
            .Configure(configure)
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "Issuer is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Audience is required.")
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ISigningKeyProvider, EphemeralSigningKeyProvider>();
        services.TryAddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>();
        services.TryAddSingleton<ITokenService, TokenService>();
        services.TryAddSingleton<IIdentityNormalizer, DefaultIdentityNormalizer>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, SigningKeyProviderGuard>());

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<AuthenticationOptions>, ISigningKeyProvider>((options, authOptions, keyProvider) =>
            {
                options.RequireHttpsMetadata = true;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = authOptions.Value.Issuer,
                    ValidAudience = authOptions.Value.Audience,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    IssuerSigningKeyResolver = (_, _, _, _) => keyProvider.GetValidationKeys()
                };
            });

        services.AddAuthorization();
        services.AddDevelopmentBypassGuard();
        return services;
    }
}
