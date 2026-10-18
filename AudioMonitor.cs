using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace PulseSlide;

public sealed class AudioMonitor : IDisposable
{
    private WasapiLoopbackCapture? _capture;
    private double _level;
    public double Level => Volatile.Read(ref _level);

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

                Volatile.Write(ref _level, samples > 0 ? Math.Sqrt(sum / samples) : 0);
            };
            _capture.StartRecording();
        }
        catch
        {
            Volatile.Write(ref _level, 0);
        }
    }

    public void Dispose()
    {
        try { _capture?.StopRecording(); } catch { }
        _capture?.Dispose();
    }
}