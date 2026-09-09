using NAudio.Wave;

namespace WardogsTacticalRadio.Audio;

public sealed class RadioAudioService : IDisposable
{
    private readonly WaveInEvent _input;
    private readonly WaveOutEvent _output;
    private readonly BufferedWaveProvider _playbackBuffer;
    private bool _disposed;

    public event Action<byte[]>? MicrophoneFrameReady;

    public RadioAudioService()
    {
        var format = new WaveFormat(16000, 16, 1);

        _input = new WaveInEvent
        {
            WaveFormat = format,
            BufferMilliseconds = 40,
            NumberOfBuffers = 3
        };
        _input.DataAvailable += (_, e) =>
        {
            if (e.BytesRecorded <= 0) return;
            var copy = new byte[e.BytesRecorded];
            Buffer.BlockCopy(e.Buffer, 0, copy, 0, e.BytesRecorded);
            MicrophoneFrameReady?.Invoke(copy);
        };

        _playbackBuffer = new BufferedWaveProvider(format)
        {
            BufferDuration = TimeSpan.FromSeconds(2),
            DiscardOnBufferOverflow = true
        };
        _output = new WaveOutEvent { DesiredLatency = 100 };
        _output.Init(_playbackBuffer);
    }

    public void Start()
    {
        _input.StartRecording();
        _output.Play();
    }

    public void Play(byte[] pcm16)
    {
        if (_disposed || pcm16.Length == 0) return;
        _playbackBuffer.AddSamples(pcm16, 0, pcm16.Length);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _input.StopRecording(); } catch { }
        try { _output.Stop(); } catch { }
        _input.Dispose();
        _output.Dispose();
    }
}
