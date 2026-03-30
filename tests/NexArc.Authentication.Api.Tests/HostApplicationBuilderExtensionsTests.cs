using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexArc.Authentication.Api.Extensions;
using NexArc.Authentication.Api.Services;

namespace NexArc.Authentication.Api.Tests;

public class HostApplicationBuilderExtensionsTests
{
    [Fact]
    public void Uses_Ephemeral_Key_In_Development_When_Signing_Key_Is_Missing()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Development
        });
        builder.Configuration["Auth:Issuer"] = "https://issuer.example.local";
        builder.Configuration["Auth:Audience"] = "example-api";

        builder.AddApiAuthentication(builder.Configuration.GetRequiredSection("Auth"));

        using var services = builder.Services.BuildServiceProvider();
        Assert.IsType<EphemeralSigningKeyProvider>(services.GetRequiredService<ISigningKeyProvider>());
    }

    [Fact]
    public void Requires_A_Durable_Signing_Key_Outside_Development()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });
        builder.Configuration["Auth:Issuer"] = "https://issuer.example.local";
        builder.Configuration["Auth:Audience"] = "example-api";

        builder.AddApiAuthentication(builder.Configuration.GetRequiredSection("Auth"));

        using var services = builder.Services.BuildServiceProvider();
        Assert.Throws<Microsoft.Extensions.Options.OptionsValidationException>(() =>
            services.GetRequiredService<ISigningKeyProvider>());
    }

    [Fact]
    public void Registers_X509_Signing_Key_When_Configured()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });
        builder.Configuration["Auth:Issuer"] = "https://issuer.example.local";
        builder.Configuration["Auth:Audience"] = "example-api";
        builder.Configuration["Auth:SigningKey:PfxBase64"] = CreateTestCertificatePfxBase64();

        builder.AddApiAuthentication(builder.Configuration.GetRequiredSection("Auth"));

        using var services = builder.Services.BuildServiceProvider();
        var signingKeyProvider = services.GetRequiredService<ISigningKeyProvider>();

        Assert.IsNotType<EphemeralSigningKeyProvider>(signingKeyProvider);
        Assert.NotNull(signingKeyProvider.GetSigningCredentials());
        Assert.NotEmpty(signingKeyProvider.GetValidationKeys());
    }

    private static string CreateTestCertificatePfxBase64()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=auth-tests", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
        return Convert.ToBase64String(certificate.Export(X509ContentType.Pfx));
    }
}
