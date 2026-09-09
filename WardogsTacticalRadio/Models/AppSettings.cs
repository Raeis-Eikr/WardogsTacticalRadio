namespace WardogsTacticalRadio.Models;

public sealed class AppSettings
{
    public string UserName { get; set; } = Environment.UserName;
    public string Callsign { get; set; } = "ALPHA-1";
    public string Role { get; set; } = "Squad Leader";
    public bool HostEligible { get; set; } = true;
    public int ListenPort { get; set; } = 47825;
    public List<FriendEntry> Friends { get; set; } = new();
    public List<KnownServer> KnownServers { get; set; } = new();
    public List<RecentSession> RecentSessions { get; set; } = new();
}

public sealed class FriendEntry
{
    public string DisplayName { get; set; } = string.Empty;
    public string Callsign { get; set; } = string.Empty;
    public string Status { get; set; } = "Offline";
}

public sealed class KnownServer
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool Favorite { get; set; }
    public DateTime LastSeenUtc { get; set; }
}

public sealed class RecentSession
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime LastJoinedUtc { get; set; }
}
