using System.Net;
using NexArc.Authentication.Client.Services;

namespace NexArc.Authentication.Client.Tests;

public class ApiBearerTokenHandlerTests
{
    [Fact]
    public async Task Adds_Authorization_Header_When_Token_Present()
    {
        var handler = new ApiBearerTokenHandler(new StaticTokenProvider("token-123"))
        {
            InnerHandler = new CaptureHandler()
        };

        var client = new HttpClient(handler);
        var response = await client.GetAsync("https://example.test/");

        var capture = (CaptureHandler)handler.InnerHandler!;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Bearer", capture.AuthorizationScheme);
        Assert.Equal("token-123", capture.AuthorizationParameter);
    }

    private sealed class StaticTokenProvider : IApiAccessTokenProvider
    {
        private readonly string _token;

        public StaticTokenProvider(string token) => _token = token;

        public Task<string?> GetAccessTokenAsync(CancellationToken ct)
            => Task.FromResult<string?>(_token);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string? AuthorizationScheme { get; private set; }
        public string? AuthorizationParameter { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var header = request.Headers.Authorization;
            AuthorizationScheme = header?.Scheme;
            AuthorizationParameter = header?.Parameter;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
