using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Examples.ServiceDefaults;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Abstractions.Models;
using NexArc.Authentication.Client.Extensions;
using NexArc.Authentication.Client.Options;
using NexArc.Authentication.Client.Services;
using NexArc.Authentication.DevicePairing.Extensions;
using NexArc.Authentication.DevBypass.Services;

var builder = WebApplication.CreateBuilder(args);
var auth = builder.Configuration.GetRequiredSection("Auth");
var devicePairing = auth.GetRequiredSection("Providers").GetRequiredSection("DevicePairing");

builder.AddClientAuthentication(auth);
builder.Services.AddProviderDevicePairing(devicePairing);

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
        environment,
        new ExamplePageAction("Device Pairing Demo", "/device-pairing"));

    var html = ExampleAuthenticationPages.BuildWhoAmIPage("Device Pairing Client", json, actions);
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
        return Results.BadRequest("Code is required.");

    var client = httpClientFactory.CreateClient(clientOptions.Value.ApiClientName);
    var providerKey = clientOptions.Value.ProviderKey;
    var response = await client.PostAsJsonAsync($"/auth/{providerKey}/resolve", new { code }, ct);
    if (!response.IsSuccessStatusCode)
        return Results.StatusCode((int)response.StatusCode);

    var tokenPair = await response.Content.ReadFromJsonAsync<TokenPair>(cancellationToken: ct);
    if (tokenPair is null)
        return Results.Problem("Invalid response from API.");

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
