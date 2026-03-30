using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NexArc.Authentication.Abstractions.Options;
using NexArc.Authentication.Api.Configuration;
using NexArc.Authentication.Api.Services;
using NexArc.Authentication.DevBypass.Extensions;

namespace NexArc.Authentication.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        Action<ApiAuthenticationOptions> configure)
    {
        services.AddOptions<ApiAuthenticationOptions>()
            .Configure(configure)
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "Issuer is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Audience is required.")
            .Validate(options => options.AccessTokenLifetime > TimeSpan.Zero, "AccessTokenLifetime must be greater than zero.")
            .Validate(options => !options.RefreshTokensEnabled || options.RefreshTokenLifetime > TimeSpan.Zero, "RefreshTokenLifetime must be greater than zero when refresh tokens are enabled.")
            .Validate(
                options => options.SessionAbsoluteLifetime is null || options.SessionAbsoluteLifetime > TimeSpan.Zero,
                "SessionAbsoluteLifetime must be greater than zero when configured.")
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
            .Configure<IOptions<ApiAuthenticationOptions>, ISigningKeyProvider>((options, authOptions, keyProvider) =>
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

    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfigurationSection authSection)
    {
        services.AddOptions<CertificateSigningKeyOptions>()
            .Bind(authSection.GetSection("SigningKey"))
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<CertificateSigningKeyOptions>, CertificateSigningKeyOptionsValidator>());

        services.AddApiAuthentication(options => authSection.Bind(options));
        services.Replace(ServiceDescriptor.Singleton<ISigningKeyProvider>(sp =>
        {
            var environment = sp.GetRequiredService<IHostEnvironment>();
            var signingKeyOptions = sp.GetRequiredService<IOptions<CertificateSigningKeyOptions>>().Value;
            var hasSigningKey = !string.IsNullOrWhiteSpace(signingKeyOptions.PfxBase64);
            if (!environment.IsDevelopment() || hasSigningKey)
                return new X509CertificateSigningKeyProvider(Options.Create(signingKeyOptions));

            return new EphemeralSigningKeyProvider();
        }));

        return services;
    }
}
