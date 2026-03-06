namespace NexArc.Authentication.Abstractions.Options;

public sealed class AuthenticationOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromHours(16);
    public bool RefreshTokensEnabled { get; set; } = true;
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromHours(16);
    public TimeSpan? SessionAbsoluteLifetime { get; set; } = TimeSpan.FromDays(7);
}
