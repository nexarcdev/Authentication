namespace NexArc.Authentication.Client.Services;

public interface IApiAccessTokenProvider
{
    Task<string?> GetAccessTokenAsync(CancellationToken ct);
}
