namespace NexArc.Authentication.DevBypass.Models;

public sealed class DevBypassUsersOptions
{
    public bool Enabled { get; set; }
    public List<DevBypassUser> Users { get; set; } = new();
}
