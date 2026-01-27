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
using NexArc.Authentication.DevicePairing.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddClientAuthentication(options =>
    {
        options.ProviderKey = builder.Configuration["Auth:ProviderKey"] ?? "device-pairing";
        options.ApiBaseUrl = builder.Configuration["Auth:ApiBaseUrl"] ?? "https://localhost:5001";
    })
    .AddProviderDevicePairing(builder.Configuration.GetSection("Auth:Providers:DevicePairing"));

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

    var html = BuildWhoAmIPage("Device Pairing Client", json, actions);
    return Results.Content(html, "text/html");
});

app.MapGet("/device-pairing", () =>
{
    var html = BuildDevicePairingDemoPage();
    return Results.Content(html, "text/html");
});

app.MapGet("/device-pairing/resolve", async (
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
    var response = await client.PostAsJsonAsync($"/auth/{providerKey}/resolve", new { code }, ct);
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

app.MapGet("/device-pairing/qr/{code}", async (
    string code,
    IHttpClientFactory httpClientFactory,
    IOptions<ClientAuthenticationOptions> clientOptions,
    CancellationToken ct) =>
{
    var client = httpClientFactory.CreateClient(clientOptions.Value.ApiClientName);
    var providerKey = clientOptions.Value.ProviderKey;
    var response = await client.GetAsync($"/auth/{providerKey}/qr/{Uri.EscapeDataString(code)}", ct);
    var content = await response.Content.ReadAsStringAsync(ct);
    return Results.Content(content, "application/json");
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

    actions.Add(new WhoAmIAction("Device Pairing Demo", "/device-pairing"));

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

static string BuildDevicePairingDemoPage()
{
    var sb = new StringBuilder();
    sb.AppendLine("<!doctype html>");
    sb.AppendLine("<html lang=\"en\">");
    sb.AppendLine("<head>");
    sb.AppendLine("  <meta charset=\"utf-8\" />");
    sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
    sb.AppendLine("  <title>Device Pairing Demo</title>");
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
    sb.AppendLine("    <h1>Device Pairing Demo</h1>");
    sb.AppendLine("    <p>Request a pairing code and resolve it.</p>");
    sb.AppendLine("    <form id=\"codeForm\">");
    sb.AppendLine("      <label>Device Id</label><br/>");
    sb.AppendLine("      <input name=\"deviceId\" value=\"device-123\" />");
    sb.AppendLine("      <button type=\"submit\">Request Code</button>");
    sb.AppendLine("    </form>");
    sb.AppendLine("    <pre id=\"codeResult\"></pre>");
    sb.AppendLine("    <button id=\"loadQr\">Load QR Payload</button>");
    sb.AppendLine("    <pre id=\"qrResult\"></pre>");
    sb.AppendLine("    <form id=\"resolveForm\">");
    sb.AppendLine("      <label>Code</label><br/>");
    sb.AppendLine("      <input name=\"code\" id=\"resolveCode\" />");
    sb.AppendLine("      <button type=\"submit\">Resolve Code</button>");
    sb.AppendLine("    </form>");
    sb.AppendLine("    <pre id=\"resolveResult\"></pre>");
    sb.AppendLine("    <p><a href=\"/\">Back to whoami</a></p>");
    sb.AppendLine("  </div>");
    sb.AppendLine("  <script>");
    sb.AppendLine("    const codeForm = document.getElementById('codeForm');");
    sb.AppendLine("    const resolveForm = document.getElementById('resolveForm');");
    sb.AppendLine("    const codeResult = document.getElementById('codeResult');");
    sb.AppendLine("    const qrResult = document.getElementById('qrResult');");
    sb.AppendLine("    const resolveResult = document.getElementById('resolveResult');");
    sb.AppendLine("    const resolveCode = document.getElementById('resolveCode');");
    sb.AppendLine("    document.getElementById('loadQr').addEventListener('click', async () => {");
    sb.AppendLine("      const code = resolveCode.value;");
    sb.AppendLine("      if (!code) { qrResult.textContent = 'Enter a code first.'; return; }");
    sb.AppendLine("      const res = await fetch(`/device-pairing/qr/${code}`);");
    sb.AppendLine("      qrResult.textContent = await res.text();");
    sb.AppendLine("    });");
    sb.AppendLine("    codeForm.addEventListener('submit', async (e) => {");
    sb.AppendLine("      e.preventDefault();");
    sb.AppendLine("      const deviceId = codeForm.deviceId.value;");
    sb.AppendLine("      const res = await fetch('/device-pairing/code', {");
    sb.AppendLine("        method: 'POST',");
    sb.AppendLine("        headers: { 'Content-Type': 'application/json' },");
    sb.AppendLine("        body: JSON.stringify({ deviceId })");
    sb.AppendLine("      });");
    sb.AppendLine("      const text = await res.text();");
    sb.AppendLine("      codeResult.textContent = text;");
    sb.AppendLine("      try { const data = JSON.parse(text); if (data.code) resolveCode.value = data.code; } catch {}");
    sb.AppendLine("    });");
    sb.AppendLine("    resolveForm.addEventListener('submit', async (e) => {");
    sb.AppendLine("      e.preventDefault();");
    sb.AppendLine("      const code = resolveForm.code.value;");
    sb.AppendLine("      const res = await fetch('/device-pairing/resolve', {");
    sb.AppendLine("        method: 'POST',");
    sb.AppendLine("        headers: { 'Content-Type': 'application/json' },");
    sb.AppendLine("        body: JSON.stringify({ code })");
    sb.AppendLine("      });");
    sb.AppendLine("      if (res.ok) { window.location.href = '/'; return; }");
    sb.AppendLine("      resolveResult.textContent = await res.text();");
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
