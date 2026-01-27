namespace NexArc.Authentication.MagicLink.Services;

public interface IMagicLinkNotifier
{
    Task SendAsync(string destination, string code, string link, CancellationToken ct);
}
