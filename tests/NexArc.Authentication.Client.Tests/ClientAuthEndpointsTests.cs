using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Abstractions.Models;
using NexArc.Authentication.Client.Extensions;
using NexArc.Authentication.Client.Options;
using NexArc.Authentication.Client.Services;

namespace NexArc.Authentication.Client.Tests;

public class ClientAuthEndpointsTests
{
    [Fact]
    public async Task Login_ReturnsNotFound_WhenSchemeMissing()
    {
        await using var app = await BuildAppAsync(includeScheme: false);
        var client = app.GetTestClient();

        var response = await client.GetAsync("/login");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Login_ChallengesWithRedirect_WhenSchemePresent()
    {
        await using var app = await BuildAppAsync(includeScheme: true);
        var client = app.GetTestClient();

        var response = await client.GetAsync("/login?returnUrl=/dashboard");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/dashboard", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Logout_ClearsTokenStore_AndRedirects()
    {
        await using var app = await BuildAppAsync(includeScheme: false);
        var tokenStore = app.Services.GetRequiredService<ITokenStore>();
        await tokenStore.SetAsync(new NexArc.Authentication.Abstractions.Models.TokenPair
        {
            AccessToken = "token",
            AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5)
        }, CancellationToken.None);

        var client = app.GetTestClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["returnUrl"] = "/after"
        });
        var response = await client.PostAsync("/logout", content);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/after", response.Headers.Location?.OriginalString);
        Assert.Null(await tokenStore.GetAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Logout_UsesUpstreamSignOut_WhenEnabled()
    {
        await using var app = await BuildAppAsync(includeScheme: true, enableUpstreamSignOut: true);
        var client = app.GetTestClient();

        var response = await client.GetAsync("/logout?returnUrl=/after");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/after", response.Headers.Location?.OriginalString);
        Assert.Equal("true", response.Headers.GetValues("X-Upstream-SignOut").Single());
    }

    private static async Task<WebApplication> BuildAppAsync(bool includeScheme, bool enableUpstreamSignOut = false)
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
            options.UpstreamSignOutEnabled = enableUpstreamSignOut;
        });

        if (includeScheme)
        {
            builder.Services.AddSingleton(new ProviderDescriptor("test-provider", "test-scheme"));
            builder.Services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestChallengeHandler>("test-scheme", _ => { });
        }
        else
        {
            builder.Services.AddSingleton(new ProviderDescriptor("test-provider", "missing-scheme"));
        }

        var app = builder.Build();
        app.MapClientAuthentication();
        await app.StartAsync();

        return app;
    }

    private sealed class TestChallengeHandler : AuthenticationHandler<AuthenticationSchemeOptions>, IAuthenticationSignOutHandler
    {
        #pragma warning disable CS0618
        public TestChallengeHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            System.Text.Encodings.Web.UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }
        #pragma warning restore CS0618

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            => Task.FromResult(AuthenticateResult.NoResult());

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status302Found;
            Response.Headers.Location = properties.RedirectUri ?? "/";
            return Task.CompletedTask;
        }

        public Task SignOutAsync(AuthenticationProperties? properties)
        {
            Response.Headers.Append("X-Upstream-SignOut", "true");
            Response.StatusCode = StatusCodes.Status302Found;
            Response.Headers.Location = properties?.RedirectUri ?? "/";
            return Task.CompletedTask;
        }
    }
}
