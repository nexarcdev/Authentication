using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace NexArc.Authentication.Api.Configuration;

internal sealed class CertificateSigningKeyOptionsValidator(IHostEnvironment environment) : IValidateOptions<CertificateSigningKeyOptions>
{
    public ValidateOptionsResult Validate(string? name, CertificateSigningKeyOptions options)
    {
        var hasSigningKey = !string.IsNullOrWhiteSpace(options.PfxBase64);
        if (!environment.IsDevelopment() && !hasSigningKey)
            return ValidateOptionsResult.Fail($"'{CertificateSigningKeyOptions.SectionName}:PfxBase64' is required outside Development.");

        return ValidateOptionsResult.Success;
    }
}
