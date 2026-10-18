using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace PulseSlide;

public sealed class AudioMonitor : IDisposable
{
    private WasapiLoopbackCapture? _capture;
    private volatile double _level;
    public double Level => _level;

    public void Start()
    {
        try
        {
            _capture = new WasapiLoopbackCapture();
            _capture.DataAvailable += (_, e) =>
            {
                var bytes = e.Buffer;
                double sum = 0;
                int samples = e.BytesRecorded / 4;
                for (int i = 0; i < samples; i++)
                {
                    float sample = BitConverter.ToSingle(bytes, i * 4);
                    sum += sample * sample;
                }
                _level = samples > 0 ? Math.Sqrt(sum / samples) : 0;
            };
            _capture.RecordingStopped += (_, _) => { };
            _capture.StartRecording();
        }
        catch
        {
            _level = 0;
        }
    }

    public void Dispose()
    {
        try { _capture?.StopRecording(); } catch { }
        _capture?.Dispose();
    }
}