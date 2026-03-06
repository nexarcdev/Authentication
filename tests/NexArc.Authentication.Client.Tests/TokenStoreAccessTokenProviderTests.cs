using System.Net.Http.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Abstractions.Models;
using NexArc.Authentication.Client.Options;
using NexArc.Authentication.Client.Services;

namespace NexArc.Authentication.Client.Tests;

public class TokenStoreAccessTokenProviderTests
{
    [Fact]
    public async Task Returns_Null_When_Expired()
    {
        var store = new InMemoryTokenStore();
        await store.SetAsync(new TokenPair
        {
            AccessToken = "expired",
            AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        }, CancellationToken.None);

        var provider = BuildProvider(store, new StubHttpClientFactory(new NoOpHandler()));
        var token = await provider.GetAccessTokenAsync(CancellationToken.None);

        Assert.Null(token);
    }

    [Fact]
    public async Task Returns_Token_When_Valid()
    {
        var store = new InMemoryTokenStore();
        await store.SetAsync(new TokenPair
        {
            AccessToken = "valid",
            AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5)
        }, CancellationToken.None);

        var provider = BuildProvider(store, new StubHttpClientFactory(new NoOpHandler()));
        var token = await provider.GetAccessTokenAsync(CancellationToken.None);

        Assert.Equal("valid", token);
    }

    [Fact]
    public async Task Refreshes_Access_Token_When_Expired_And_Refresh_Token_Is_Valid()
    {
        var now = new DateTimeOffset(2026, 3, 5, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var store = new InMemoryTokenStore();
        await store.SetAsync(new TokenPair
        {
            AccessToken = "expired",
            AccessTokenExpiresAt = now.AddMinutes(-1),
            RefreshToken = "refresh-token",
            RefreshTokenExpiresAt = now.AddHours(1)
        }, CancellationToken.None);

        var refreshHandler = new RefreshHandler(new TokenPair
        {
            AccessToken = "new-access",
            AccessTokenExpiresAt = now.AddMinutes(30),
            RefreshToken = "new-refresh",
            RefreshTokenExpiresAt = now.AddHours(2)
        });

        var factory = new StubHttpClientFactory(refreshHandler);
        var provider = BuildProvider(store, factory, timeProvider);

        var token = await provider.GetAccessTokenAsync(CancellationToken.None);

        Assert.Equal("new-access", token);
        Assert.Equal(1, refreshHandler.CallCount);
        Assert.Equal("ApiAuth", factory.LastClientName);

        var stored = await store.GetAsync(CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal("new-access", stored!.AccessToken);
    }

    [Fact]
    public async Task Keeps_Current_Token_When_Refresh_Fails_And_Access_Still_Valid()
    {
        var now = new DateTimeOffset(2026, 3, 5, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var store = new InMemoryTokenStore();
        await store.SetAsync(new TokenPair
        {
            AccessToken = "still-valid",
            AccessTokenExpiresAt = now.AddSeconds(30),
            RefreshToken = "refresh-token",
            RefreshTokenExpiresAt = now.AddHours(1)
        }, CancellationToken.None);

        var refreshHandler = new FailingHandler();
        var provider = BuildProvider(store, new StubHttpClientFactory(refreshHandler), timeProvider);

        var token = await provider.GetAccessTokenAsync(CancellationToken.None);

        Assert.Equal("still-valid", token);
    }

    private static TokenStoreAccessTokenProvider BuildProvider(
        ITokenStore store,
        IHttpClientFactory factory,
        TimeProvider? timeProvider = null)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new ClientAuthenticationOptions
        {
            ProviderKey = "test-provider",
            ApiBaseUrl = "https://api.example.local",
            AuthApiClientName = "ApiAuth",
            AutomaticTokenRefreshEnabled = true,
            RefreshBeforeExpiry = TimeSpan.FromMinutes(1)
        });

        return new TokenStoreAccessTokenProvider(
            store,
            factory,
            options,
            timeProvider ?? TimeProvider.System,
            NullLogger<TokenStoreAccessTokenProvider>.Instance);
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public StubHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public string? LastClientName { get; private set; }

        public HttpClient CreateClient(string name)
        {
            LastClientName = name;
            return new HttpClient(_handler, disposeHandler: false)
            {
                BaseAddress = new Uri("https://api.example.local")
            };
        }
    }

    private sealed class NoOpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest));
    }

    private sealed class RefreshHandler : HttpMessageHandler
    {
        private readonly TokenPair _response;

        public RefreshHandler(TokenPair response)
        {
            _response = response;
        }

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;

            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = JsonContent.Create(_response)
            };

            return Task.FromResult(response);
        }
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized));
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FakeTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
            => _utcNow;
    }
}
