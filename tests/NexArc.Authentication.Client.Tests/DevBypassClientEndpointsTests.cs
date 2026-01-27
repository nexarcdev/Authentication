using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexArc.Authentication.Abstractions.Models;
using NexArc.Authentication.Client.Extensions;
using NexArc.Authentication.Client.Options;
using NexArc.Authentication.Client.Services;
using NexArc.Authentication.DevBypass.Models;
using NexArc.Authentication.DevBypass.Services;

namespace NexArc.Authentication.Client.Tests;

public class DevBypassClientEndpointsTests
{
    [Fact]
    public async Task DevLoginPage_ReturnsHtml_WhenEnabled()
    {
        await using var app = await BuildAppAsync(enabled: true);
        var environment = app.Services.GetRequiredService<IHostEnvironment>();
        var provider = app.Services
            .GetServices<IDevelopmentBypassUserProvider>()
            .FirstOrDefault(p => p.ProviderKey == "test-provider");

        Assert.True(environment.IsDevelopment());
        Assert.NotNull(provider);
        Assert.True(provider!.Enabled);


        var client = app.GetTestClient();

        var response = await client.GetAsync("/auth/dev-login?returnUrl=/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Dev Login", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Continue as", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("returnUrl", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DevLoginPage_ReturnsNotFound_WhenDisabled()
    {
        await using var app = await BuildAppAsync(enabled: false);
        var client = app.GetTestClient();

        var response = await client.GetAsync("/auth/dev-login");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DevLogin_Post_SavesToken()
    {
        await using var app = await BuildAppAsync(enabled: true);
        var client = app.GetTestClient();

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["user"] = "dev.user@example.com",
            ["returnUrl"] = "/after-login"
        });

        var response = await client.PostAsync("/auth/dev-login", content);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/after-login", response.Headers.Location?.OriginalString);

        var tokenStore = app.Services.GetRequiredService<ITokenStore>();
        var tokenPair = await tokenStore.GetAsync(CancellationToken.None);

        Assert.NotNull(tokenPair);
        Assert.Equal("dev-access-token", tokenPair!.AccessToken);
    }

    private static async Task<WebApplication> BuildAppAsync(bool enabled)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseTestServer();

        builder.Services.AddClientAuthentication(options =>
        {
            options.ProviderKey = "test-provider";
            options.ApiBaseUrl = "http://localhost";
        });

        builder.Services.AddSingleton<IDevelopmentBypassUserProvider>(
            new TestDevBypassProvider("test-provider", enabled));

        builder.Services.AddSingleton<IHttpClientFactory>(sp =>
            new StubHttpClientFactory(BuildApiClient()));

        var app = builder.Build();
        app.MapClientAuthentication();
        await app.StartAsync();

        return app;
    }

    private static HttpClient BuildApiClient()
    {
        var handler = new StubMessageHandler();
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };
    }

    private sealed class TestDevBypassProvider : IDevelopmentBypassUserProvider
    {
        public TestDevBypassProvider(string providerKey, bool enabled)
        {
            ProviderKey = providerKey;
            Enabled = enabled;
            Users = new List<DevBypassUser>
            {
                new()
                {
                    Subject = "dev-user",
                    Email = "dev.user@example.com",
                    Name = "Dev User",
                    Roles = new[] { "Staff" }
                }
            };
        }

        public string ProviderKey { get; }
        public bool Enabled { get; }
        public IReadOnlyList<DevBypassUser> Users { get; }
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public StubHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class StubMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var tokenPair = new TokenPair
            {
                AccessToken = "dev-access-token",
                AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
            };

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(tokenPair)
            });
        }
    }
}
