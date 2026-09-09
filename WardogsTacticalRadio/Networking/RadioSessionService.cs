using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using WardogsTacticalRadio.Audio;
using WardogsTacticalRadio.Models;

namespace WardogsTacticalRadio.Networking;

public sealed class RadioSessionService : IAsyncDisposable
{
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    private readonly List<TcpClient> _clients = new();
    private readonly SemaphoreSlim _sendGate = new(1, 1);
    private TcpListener? _listener;
    private TcpClient? _upstreamClient;
    private CancellationTokenSource? _cts;

    public bool IsHosting { get; private set; }
    public bool IsConnected { get; private set; }
    public RadioSessionState? Session { get; private set; }
    public RadioPeer LocalPeer { get; }

    public event Action<string>? StatusChanged;
    public event Action<RadioSessionState>? SessionChanged;
    public event Action<AudioFrame>? AudioFrameReceived;

    public RadioSessionService(AppSettings settings)
    {
        LocalPeer = new RadioPeer
        {
            UserName = settings.UserName,
            Callsign = settings.Callsign,
            Role = settings.Role,
            HostEligible = settings.HostEligible
        };
    }

    public async Task HostAsync(string sessionName, int port, CancellationToken cancellationToken = default)
    {
        await StopAsync();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Session = new RadioSessionState
        {
            SessionName = string.IsNullOrWhiteSpace(sessionName) ? "WARDOGS RADIO NET" : sessionName.Trim(),
            HostPeerId = LocalPeer.PeerId,
            HostCallsign = LocalPeer.Callsign,
            Peers = [LocalPeer]
        };

        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        IsHosting = true;
        IsConnected = true;
        StatusChanged?.Invoke($"HOSTING // {GetBestLanAddress()}:{port}");
        SessionChanged?.Invoke(Session);
        _ = AcceptLoopAsync(_cts.Token);
    }

    public async Task JoinAsync(string address, int port, CancellationToken cancellationToken = default)
    {
        await StopAsync();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var client = new TcpClient { NoDelay = true };
        StatusChanged?.Invoke($"CONNECTING // {address}:{port}");
        await client.ConnectAsync(address, port, _cts.Token);
        _upstreamClient = client;
        IsConnected = true;

        await SendAsync(client, NetworkEnvelope.Create("hello", LocalPeer), _cts.Token);
        _ = ReceiveLoopAsync(client, _cts.Token);
        StatusChanged?.Invoke("CONNECTED // awaiting session sync");
    }

    public async Task TransmitAudioAsync(string channelId, byte[] pcm16, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || pcm16.Length == 0) return;
        var frame = new AudioFrame
        {
            SourcePeerId = LocalPeer.PeerId,
            SourceCallsign = LocalPeer.Callsign,
            ChannelId = channelId,
            Pcm16 = pcm16
        };
        var envelope = NetworkEnvelope.Create("audio", frame);

        if (IsHosting)
        {
            await BroadcastAsync(envelope, null, cancellationToken);
        }
        else if (_upstreamClient is { Connected: true })
        {
            await SendAsync(_upstreamClient, envelope, cancellationToken);
        }
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        if (_listener is null) return;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                client.NoDelay = true;
                lock (_clients) _clients.Add(client);
                _ = ReceiveLoopAsync(client, cancellationToken);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { StatusChanged?.Invoke($"HOST ERROR // {ex.Message}"); }
    }

    private async Task ReceiveLoopAsync(TcpClient client, CancellationToken cancellationToken)
    {
        try
        {
            using var reader = new StreamReader(client.GetStream(), Encoding.UTF8, false, 4096, leaveOpen: true);
            while (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line is null) break;
                var envelope = JsonSerializer.Deserialize<NetworkEnvelope>(line, _json);
                if (envelope is null) continue;
                await HandleEnvelopeAsync(client, envelope, cancellationToken);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { StatusChanged?.Invoke($"LINK LOST // {ex.Message}"); }
        finally
        {
            lock (_clients) _clients.Remove(client);
            try { client.Dispose(); } catch { }
        }
    }

    private async Task HandleEnvelopeAsync(TcpClient source, NetworkEnvelope envelope, CancellationToken cancellationToken)
    {
        switch (envelope.Type)
        {
            case "hello" when IsHosting && Session is not null:
            {
                var peer = envelope.ReadPayload<RadioPeer>();
                if (peer is null) return;
                peer.LastHeartbeatUtc = DateTime.UtcNow;
                Session.Peers.RemoveAll(p => p.PeerId == peer.PeerId);
                Session.Peers.Add(peer);
                await SendAsync(source, NetworkEnvelope.Create("session", Session), cancellationToken);
                await BroadcastSessionAsync(cancellationToken);
                SessionChanged?.Invoke(Session);
                StatusChanged?.Invoke($"LINKED // {peer.Callsign}");
                break;
            }
            case "session":
            {
                var state = envelope.ReadPayload<RadioSessionState>();
                if (state is null) return;
                Session = state;
                SessionChanged?.Invoke(state);
                StatusChanged?.Invoke($"NET SYNC // {state.SessionName}");
                break;
            }
            case "audio":
            {
                var frame = envelope.ReadPayload<AudioFrame>();
                if (frame is null || frame.SourcePeerId == LocalPeer.PeerId) return;
                AudioFrameReceived?.Invoke(frame);
                if (IsHosting)
                    await BroadcastAsync(envelope, source, cancellationToken);
                break;
            }
        }
    }

    private async Task BroadcastSessionAsync(CancellationToken cancellationToken)
    {
        if (Session is null) return;
        await BroadcastAsync(NetworkEnvelope.Create("session", Session), null, cancellationToken);
    }

    private async Task BroadcastAsync(NetworkEnvelope envelope, TcpClient? except, CancellationToken cancellationToken)
    {
        List<TcpClient> snapshot;
        lock (_clients) snapshot = _clients.ToList();
        foreach (var client in snapshot)
        {
            if (client == except || !client.Connected) continue;
            try { await SendAsync(client, envelope, cancellationToken); } catch { }
        }
    }

    private async Task SendAsync(TcpClient client, NetworkEnvelope envelope, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(envelope, _json) + "\n";
        var bytes = Encoding.UTF8.GetBytes(json);
        await _sendGate.WaitAsync(cancellationToken);
        try
        {
            await client.GetStream().WriteAsync(bytes, cancellationToken);
            await client.GetStream().FlushAsync(cancellationToken);
        }
        finally { _sendGate.Release(); }
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        try { _listener?.Stop(); } catch { }
        try { _upstreamClient?.Dispose(); } catch { }
        lock (_clients)
        {
            foreach (var client in _clients) try { client.Dispose(); } catch { }
            _clients.Clear();
        }
        _listener = null;
        _upstreamClient = null;
        _cts?.Dispose();
        _cts = null;
        IsHosting = false;
        IsConnected = false;
        Session = null;
        await Task.CompletedTask;
    }

    public static string GetBestLanAddress()
    {
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces()
                         .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback))
            {
                foreach (var ua in nic.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ua.Address))
                        return ua.Address.ToString();
                }
            }
        }
        catch { }
        return "127.0.0.1";
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _sendGate.Dispose();
    }
}
