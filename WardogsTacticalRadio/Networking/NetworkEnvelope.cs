using System.Text.Json;

namespace WardogsTacticalRadio.Networking;

public sealed class NetworkEnvelope
{
    public string Type { get; set; } = string.Empty;
    public JsonElement Payload { get; set; }

    public static NetworkEnvelope Create<T>(string type, T payload)
    {
        return new NetworkEnvelope
        {
            Type = type,
            Payload = JsonSerializer.SerializeToElement(payload)
        };
    }

    public T? ReadPayload<T>() => Payload.Deserialize<T>();
}
