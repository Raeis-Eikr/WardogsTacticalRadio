namespace WardogsTacticalRadio.Models;

public sealed class RadioSessionState
{
    public Guid SessionId { get; set; } = Guid.NewGuid();
    public string SessionName { get; set; } = "WARDOGS RADIO NET";
    public Guid HostPeerId { get; set; }
    public string HostCallsign { get; set; } = string.Empty;
    public List<RadioPeer> Peers { get; set; } = new();
    public List<RadioChannel> Channels { get; set; } =
    [
        new() { Id = "CMD", Name = "Command", RequiredRole = "Squad Leader" },
        new() { Id = "SQD", Name = "Squad", RequiredRole = "Any" }
    ];
}

public sealed class RadioPeer
{
    public Guid PeerId { get; set; } = Guid.NewGuid();
    public string UserName { get; set; } = string.Empty;
    public string Callsign { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool HostEligible { get; set; }
    public DateTime LastHeartbeatUtc { get; set; } = DateTime.UtcNow;
}

public sealed class RadioChannel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RequiredRole { get; set; } = "Any";
}
