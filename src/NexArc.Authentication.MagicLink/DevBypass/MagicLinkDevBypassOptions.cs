namespace NexArc.Authentication.MagicLink.DevBypass;

public sealed class MagicLinkDevBypassOptions
{
    public bool Enabled { get; set; }
    public List<MagicLinkDevBypassDestination> Destinations { get; set; } = new();
}
