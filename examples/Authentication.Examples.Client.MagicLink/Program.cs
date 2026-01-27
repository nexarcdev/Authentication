using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Abstractions.Models;
using NexArc.Authentication.Client.Extensions;
using NexArc.Authentication.Client.Options;
using NexArc.Authentication.Client.Services;
using NexArc.Authentication.DevBypass.Services;
using NexArc.Authentication.MagicLink.Extensions;
using NexArc.Authentication.MagicLink.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddClientAuthentication(options =>
    {
        options.ProviderKey = builder.Configuration["Auth:ProviderKey"] ?? "magic-link";
        options.ApiBaseUrl = builder.Configuration["Auth:ApiBaseUrl"] ?? "https://localhost:5001";
    })
    .AddProviderMagicLink(builder.Configuration.GetSection("Auth:Providers:MagicLink"));

builder.Services.AddSingleton<InMemoryMagicLinkNotifier>();
builder.Services.AddSingleton<IMagicLinkNotifier>(sp => sp.GetRequiredService<InMemoryMagicLinkNotifier>());

builder.Services.AddAuthorization();

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
    var payload = BuildWhoAmI(context);
    var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    var actions = await BuildActionsAsync(
        schemeProvider,
        clientOptions.Value.ProviderKey,
        bypassProviders,
        environment);

    var html = BuildWhoAmIPage("Magic Link Client", json, actions);
    return Results.Content(html, "text/html");
});

app.MapGet("/magic-link", () =>
{
    var html = BuildMagicLinkDemoPage();
    return Results.Content(html, "text/html");
});

app.MapGet("/magic-link/last", (InMemoryMagicLinkNotifier notifier) =>
{
    return notifier.LastMessage is null
        ? Results.NotFound()
        : Results.Ok(notifier.LastMessage);
});

app.MapGet("/magic-link/redeem", async (
    string code,
    IHttpClientFactory httpClientFactory,
    IOptions<ClientAuthenticationOptions> clientOptions,
    ITokenStore tokenStore,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(code))
    {
        return Results.BadRequest("Code is required.");
    }

    var client = httpClientFactory.CreateClient(clientOptions.Value.ApiClientName);
    var providerKey = clientOptions.Value.ProviderKey;
    var response = await client.PostAsJsonAsync($"/auth/{providerKey}/redeem", new { code }, ct);
    if (!response.IsSuccessStatusCode)
    {
        return Results.StatusCode((int)response.StatusCode);
    }

    var tokenPair = await response.Content.ReadFromJsonAsync<TokenPair>(cancellationToken: ct);
    if (tokenPair is null)
    {
        return Results.Problem("Invalid response from API.");
    }

    await tokenStore.SetAsync(tokenPair, ct);
    return Results.Ok();
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

static async Task<IReadOnlyList<WhoAmIAction>> BuildActionsAsync(
    IAuthenticationSchemeProvider schemeProvider,
    string providerKey,
    IEnumerable<IDevelopmentBypassUserProvider> bypassProviders,
    IHostEnvironment environment)
{
    var actions = new List<WhoAmIAction>();
    var scheme = await schemeProvider.GetSchemeAsync(providerKey);
    if (scheme is not null)
    {
        actions.Add(new WhoAmIAction("Login", "/login?returnUrl=/"));
    }

    var devBypass = bypassProviders.FirstOrDefault(p =>
        string.Equals(p.ProviderKey, providerKey, StringComparison.OrdinalIgnoreCase) && p.Enabled);
    if (devBypass is not null && environment.IsDevelopment())
    {
        actions.Add(new WhoAmIAction("Dev Login", "/auth/dev-login?returnUrl=/"));
    }

    actions.Add(new WhoAmIAction("Magic Link Demo", "/magic-link"));

    return actions;
}

static object BuildWhoAmI(HttpContext context)
{
    var user = context.User;
    return new
    {
        user.Identity?.IsAuthenticated,
        user.Identity?.AuthenticationType,
        user.Identity?.Name,
        Roles = GetRoles(user),
        Claims = user.Claims.Select(c => new { c.Type, c.Value })
    };
}

static string BuildWhoAmIPage(string title, string json, IReadOnlyList<WhoAmIAction> actions)
{
    var sb = new StringBuilder();
    sb.AppendLine("<!doctype html>");
    sb.AppendLine("<html lang=\"en\">");
    sb.AppendLine("<head>");
    sb.AppendLine("  <meta charset=\"utf-8\" />");
    sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
    sb.AppendLine($"  <title>{System.Net.WebUtility.HtmlEncode(title)}</title>");
    sb.AppendLine("  <style>");
    sb.AppendLine("    body { font-family: system-ui, sans-serif; margin: 2rem; }");
    sb.AppendLine("    .card { max-width: 860px; margin: 0 auto; }");
    sb.AppendLine("    .actions { display: flex; gap: 0.75rem; flex-wrap: wrap; margin-bottom: 1rem; }");
    sb.AppendLine("    .actions a, .actions button { padding: 0.5rem 0.9rem; border: 1px solid #ddd; background: #fff; text-decoration: none; color: #111; }");
    sb.AppendLine("    pre { background: #f6f8fa; padding: 1rem; overflow: auto; }");
    sb.AppendLine("    form { margin: 0; }");
    sb.AppendLine("  </style>");
    sb.AppendLine("</head>");
    sb.AppendLine("<body>");
    sb.AppendLine("  <div class=\"card\">");
    sb.AppendLine($"    <h1>{System.Net.WebUtility.HtmlEncode(title)}</h1>");
    sb.AppendLine("    <div class=\"actions\">");

    foreach (var action in actions)
    {
        sb.AppendLine($"      <a href=\"{System.Net.WebUtility.HtmlEncode(action.Href)}\">{System.Net.WebUtility.HtmlEncode(action.Label)}</a>");
    }

    sb.AppendLine("      <form method=\"post\" action=\"/logout\">");
    sb.AppendLine("        <button type=\"submit\">Logout</button>");
    sb.AppendLine("      </form>");
    sb.AppendLine("    </div>");
    sb.AppendLine("    <pre>");
    sb.AppendLine(System.Net.WebUtility.HtmlEncode(json));
    sb.AppendLine("    </pre>");
    sb.AppendLine("  </div>");
    sb.AppendLine("</body>");
    sb.AppendLine("</html>");
    return sb.ToString();
}

static string BuildMagicLinkDemoPage()
{
    var sb = new StringBuilder();
    sb.AppendLine("<!doctype html>");
    sb.AppendLine("<html lang=\"en\">");
    sb.AppendLine("<head>");
    sb.AppendLine("  <meta charset=\"utf-8\" />");
    sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
    sb.AppendLine("  <title>Magic Link Demo</title>");
    sb.AppendLine("  <style>");
    sb.AppendLine("    body { font-family: system-ui, sans-serif; margin: 2rem; }");
    sb.AppendLine("    .card { max-width: 860px; margin: 0 auto; }");
    sb.AppendLine("    form { margin: 1rem 0; }");
    sb.AppendLine("    input, button { padding: 0.5rem; }");
    sb.AppendLine("    pre { background: #f6f8fa; padding: 1rem; overflow: auto; }");
    sb.AppendLine("  </style>");
    sb.AppendLine("</head>");
    sb.AppendLine("<body>");
    sb.AppendLine("  <div class=\"card\">");
    sb.AppendLine("    <h1>Magic Link Demo</h1>");
    sb.AppendLine("    <p>Request a magic link, view the last message, then redeem the code.</p>");
    sb.AppendLine("    <form id=\"requestForm\">");
    sb.AppendLine("      <label>Destination</label><br/>");
    sb.AppendLine("      <input name=\"destination\" value=\"dev.user@example.com\" />");
    sb.AppendLine("      <button type=\"submit\">Request Link</button>");
    sb.AppendLine("    </form>");
    sb.AppendLine("    <pre id=\"requestResult\"></pre>");
    sb.AppendLine("    <button id=\"loadLast\">Load Last Message</button>");
    sb.AppendLine("    <pre id=\"lastResult\"></pre>");
    sb.AppendLine("    <form id=\"redeemForm\">");
    sb.AppendLine("      <label>Code</label><br/>");
    sb.AppendLine("      <input name=\"code\" id=\"redeemCode\" />");
    sb.AppendLine("      <button type=\"submit\">Redeem Code</button>");
    sb.AppendLine("    </form>");
    sb.AppendLine("    <pre id=\"redeemResult\"></pre>");
    sb.AppendLine("    <p><a href=\"/\">Back to whoami</a></p>");
    sb.AppendLine("  </div>");
    sb.AppendLine("  <script>");
    sb.AppendLine("    const requestForm = document.getElementById('requestForm');");
    sb.AppendLine("    const redeemForm = document.getElementById('redeemForm');");
    sb.AppendLine("    const requestResult = document.getElementById('requestResult');");
    sb.AppendLine("    const lastResult = document.getElementById('lastResult');");
    sb.AppendLine("    const redeemResult = document.getElementById('redeemResult');");
    sb.AppendLine("    const redeemCode = document.getElementById('redeemCode');");
    sb.AppendLine("    document.getElementById('loadLast').addEventListener('click', async () => {");
    sb.AppendLine("      const res = await fetch('/magic-link/last');");
    sb.AppendLine("      if (!res.ok) { lastResult.textContent = 'No message yet.'; return; }");
    sb.AppendLine("      const data = await res.json();");
    sb.AppendLine("      lastResult.textContent = JSON.stringify(data, null, 2);");
    sb.AppendLine("      if (data.code) { redeemCode.value = data.code; }");
    sb.AppendLine("    });");
    sb.AppendLine("    requestForm.addEventListener('submit', async (e) => {");
    sb.AppendLine("      e.preventDefault();");
    sb.AppendLine("      const destination = requestForm.destination.value;");
    sb.AppendLine("      const res = await fetch('/magic-link/request', {");
    sb.AppendLine("        method: 'POST',");
    sb.AppendLine("        headers: { 'Content-Type': 'application/json' },");
    sb.AppendLine("        body: JSON.stringify({ destination })");
    sb.AppendLine("      });");
    sb.AppendLine("      const text = await res.text();");
    sb.AppendLine("      requestResult.textContent = text;");
    sb.AppendLine("    });");
    sb.AppendLine("    redeemForm.addEventListener('submit', async (e) => {");
    sb.AppendLine("      e.preventDefault();");
    sb.AppendLine("      const code = redeemForm.code.value;");
    sb.AppendLine("      const res = await fetch('/magic-link/redeem', {");
    sb.AppendLine("        method: 'POST',");
    sb.AppendLine("        headers: { 'Content-Type': 'application/json' },");
    sb.AppendLine("        body: JSON.stringify({ code })");
    sb.AppendLine("      });");
    sb.AppendLine("      if (res.ok) { window.location.href = '/'; return; }");
    sb.AppendLine("      redeemResult.textContent = await res.text();");
    sb.AppendLine("    });");
    sb.AppendLine("  </script>");
    sb.AppendLine("</body>");
    sb.AppendLine("</html>");
    return sb.ToString();
}

static IReadOnlyList<string> GetRoles(ClaimsPrincipal user)
{
    return user.FindAll(ClaimTypes.Role)
        .Select(c => c.Value)
        .Concat(user.FindAll("role").Select(c => c.Value))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
}

sealed record WhoAmIAction(string Label, string Href);

sealed class InMemoryMagicLinkNotifier : IMagicLinkNotifier
{
    private readonly object _sync = new();
    public MagicLinkNotification? LastMessage { get; private set; }

    public Task SendAsync(string destination, string code, string link, CancellationToken ct)
    {
        lock (_sync)
        {
            LastMessage = new MagicLinkNotification(destination, code, link);
        }

        return Task.CompletedTask;
    }
}

sealed record MagicLinkNotification(string Destination, string Code, string Link);
