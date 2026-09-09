namespace WardogsTacticalRadio.Audio;

public sealed class AudioFrame
{
    public Guid SourcePeerId { get; set; }
    public string SourceCallsign { get; set; } = string.Empty;
    public string ChannelId { get; set; } = "SQD";
    public byte[] Pcm16 { get; set; } = Array.Empty<byte>();
}
