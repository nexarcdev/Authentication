using System.Text.Json;
using Examples.ServiceDefaults;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Client.Extensions;
using NexArc.Authentication.Client.Options;
using NexArc.Authentication.DevBypass.Services;
using NexArc.Authentication.Provider.GoogleWorkspace.Extensions;

var builder = WebApplication.CreateBuilder(args);
var auth = builder.Configuration.GetRequiredSection("Auth");
var googleWorkspace = auth.GetRequiredSection("Providers").GetRequiredSection("GoogleWorkspace");

builder.AddOidcClientAuthentication(auth);
builder.Services.AddProviderGoogleWorkspace(googleWorkspace);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapClientAuthentication();

app.MapGet("/", async (
    HttpContext context,
    IAuthenticationSchemeProvider schemeProvider,
    IOptions<ClientAuthenticationOptions> clientOptions,
    IEnumerable<IDevelopmentBypassUserProvider> bypassProviders,
    IHostEnvironment environment) =>
{
    var payload = ExampleAuthenticationPages.BuildWhoAmIPayload(context);
    var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    var actions = await ExampleAuthenticationPages.BuildClientActionsAsync(
        schemeProvider,
        clientOptions.Value.ProviderKey,
        bypassProviders,
        environment);

    var html = ExampleAuthenticationPages.BuildWhoAmIPage("Google Workspace Client", json, actions);
    return Results.Content(html, "text/html");
});

app.MapGet("/api/me", async (
    IHttpClientFactory httpClientFactory,
    IOptions<ClientAuthenticationOptions> clientOptions,
    CancellationToken ct) =>
{
    var client = httpClientFactory.CreateClient(clientOptions.Value.ApiClientName);
    var response = await client.GetAsync("/me", ct);
    var content = await response.Content.ReadAsStringAsync(ct);
    return Results.Content(content, "application/json");
});

app.Run();
