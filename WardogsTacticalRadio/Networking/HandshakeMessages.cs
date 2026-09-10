using WardogsTacticalRadio.Models;

namespace WardogsTacticalRadio.Networking;

public sealed class ChallengeMessage
{
    public string Nonce { get; set; } = string.Empty;
}

public sealed class HelloRequest
{
    public RadioPeer Peer { get; set; } = new();
    public string ClientNonce { get; set; } = string.Empty;
    public string Proof { get; set; } = string.Empty;
}

public sealed class SessionAck
{
    public RadioSessionState Session { get; set; } = new();
    public string HostProof { get; set; } = string.Empty;
}

public sealed class AuthFailedMessage
{
    public string Reason { get; set; } = string.Empty;
}
