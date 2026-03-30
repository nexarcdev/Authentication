using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using NexArc.Authentication.DevBypass.Services;

namespace Examples.ServiceDefaults;

public static class ExampleAuthenticationPages
{
    public static async Task<IReadOnlyList<ExamplePageAction>> BuildClientActionsAsync(
        IAuthenticationSchemeProvider schemeProvider,
        string providerKey,
        IEnumerable<IDevelopmentBypassUserProvider> bypassProviders,
        IHostEnvironment environment,
        params ExamplePageAction[] additionalActions)
    {
        var actions = new List<ExamplePageAction>();
        var scheme = await schemeProvider.GetSchemeAsync(providerKey);
        if (scheme is not null)
            actions.Add(new ExamplePageAction("Login", "/login?returnUrl=/"));

        var devBypass = bypassProviders.FirstOrDefault(provider =>
            string.Equals(provider.ProviderKey, providerKey, StringComparison.OrdinalIgnoreCase) && provider.Enabled);
        if (devBypass is not null && environment.IsDevelopment())
            actions.Add(new ExamplePageAction("Dev Login", "/auth/dev-login?returnUrl=/"));

        actions.AddRange(additionalActions);
        return actions;
    }

    public static object BuildWhoAmIPayload(HttpContext context)
    {
        var user = context.User;
        return new
        {
            user.Identity?.IsAuthenticated,
            user.Identity?.AuthenticationType,
            user.Identity?.Name,
            Roles = GetRoles(user),
            Claims = user.Claims.Select(claim => new { claim.Type, claim.Value })
        };
    }

    public static string BuildWhoAmIPage(string title, string json, IReadOnlyList<ExamplePageAction> actions)
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
            sb.AppendLine($"      <a href=\"{System.Net.WebUtility.HtmlEncode(action.Href)}\">{System.Net.WebUtility.HtmlEncode(action.Label)}</a>");

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

    private static IReadOnlyList<string> GetRoles(ClaimsPrincipal user)
    {
        return user.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Concat(user.FindAll("role").Select(claim => claim.Value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

public sealed record ExamplePageAction(string Label, string Href);
