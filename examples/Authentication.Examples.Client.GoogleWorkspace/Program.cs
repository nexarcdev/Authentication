using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Client.Extensions;
using NexArc.Authentication.Client.Options;
using NexArc.Authentication.Client.Services;
using NexArc.Authentication.DevBypass.Services;
using NexArc.Authentication.Provider.GoogleWorkspace.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddClientAuthentication(options =>
    {
        options.ProviderKey = builder.Configuration["Auth:ProviderKey"] ?? "google-workspace";
        options.ApiBaseUrl = builder.Configuration["Auth:ApiBaseUrl"] ?? "https://localhost:5001";
    })
    .AddProviderGoogleWorkspace(builder.Configuration.GetSection("Auth:Providers:GoogleWorkspace"));

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

    var html = BuildWhoAmIPage("Google Workspace Client", json, actions);
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

static IReadOnlyList<string> GetRoles(ClaimsPrincipal user)
{
    return user.FindAll(ClaimTypes.Role)
        .Select(c => c.Value)
        .Concat(user.FindAll("role").Select(c => c.Value))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
}

sealed record WhoAmIAction(string Label, string Href);
