using System.Net.Http.Headers;

namespace NexArc.Authentication.Client.Services;

public sealed class ApiBearerTokenHandler : DelegatingHandler
{
    private readonly IApiAccessTokenProvider _accessTokenProvider;

    public ApiBearerTokenHandler(IApiAccessTokenProvider accessTokenProvider)
    {
        _accessTokenProvider = accessTokenProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _accessTokenProvider.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
