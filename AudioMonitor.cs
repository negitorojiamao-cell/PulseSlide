using System;
using System.Linq;
using System.Numerics;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace PulseSlide;

public sealed class AudioMonitor : IDisposable
{
    private const int FftSize = 1024;
    private const int BandCount = 48;
    private readonly float[] _ring = new float[FftSize];
    private int _writeIndex;
    private readonly object _bufferLock = new();
    private float[] _waveform = new float[FftSize];
    private float[] _bands = new float[BandCount];
    private double _level;
    private double _bassPulse;
    private WasapiLoopbackCapture? _capture;

    public double Level => Volatile.Read(ref _level);
    public double BassPulse => Volatile.Read(ref _bassPulse);
    public float[] Waveform => Volatile.Read(ref _waveform);
    public float[] Bands => Volatile.Read(ref _bands);

    public void Start()
    {
        try
        {
            _capture = new WasapiLoopbackCapture();
            _capture.DataAvailable += (_, e) =>
            {
                int channels = Math.Max(1, _capture?.WaveFormat.Channels ?? 2);
                int bytesPerSample = Math.Max(1, _capture?.WaveFormat.BitsPerSample / 8 ?? 4);
                int frameBytes = channels * bytesPerSample;
                if (bytesPerSample != 4 || _capture?.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat) return;

                double sum = 0;
                int frames = e.BytesRecorded / frameBytes;
                lock (_bufferLock)
                {
                    for (int frame = 0; frame < frames; frame++)
                    {
                        float mono = 0;
                        for (int ch = 0; ch < channels; ch++)
                            mono += BitConverter.ToSingle(e.Buffer, frame * frameBytes + ch * bytesPerSample);
                        mono /= channels;
                        if (!float.IsFinite(mono)) mono = 0;
                        _ring[_writeIndex] = mono;
                        _writeIndex = (_writeIndex + 1) % FftSize;
                        sum += mono * mono;
                    }
                }

                if (frames > 0)
                    Volatile.Write(ref _level, Math.Sqrt(sum / frames));

                Analyze();
            };
            _capture.StartRecording();
        }
        catch
        {
            Volatile.Write(ref _level, 0);
        }
    }

    private void Analyze()
    {
        float[] samples = new float[FftSize];
        lock (_bufferLock)
        {
            for (int i = 0; i < FftSize; i++)
                samples[i] = _ring[(_writeIndex + i) % FftSize];
        }

        Volatile.Write(ref _waveform, samples);
        var fft = new Complex[FftSize];
        double bassEnergy = 0;
        for (int i = 0; i < FftSize; i++)
        {
            double window = 0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (FftSize - 1));
            fft[i] = new Complex(samples[i] * window, 0);
        }

        for (int i = 0; i < FftSize / 2; i++)
        {
            int bit = ReverseBits(i, 10);
            if (bit > i) (fft[i], fft[bit]) = (fft[bit], fft[i]);
        }

        for (int size = 2; size <= FftSize; size <<= 1)
        {
            int half = size / 2;
            var root = Complex.FromPolarCoordinates(1, -2 * Math.PI / size);
            for (int start = 0; start < FftSize; start += size)
            {
                var factor = Complex.One;
                for (int j = 0; j < half; j++)
                {
                    var even = fft[start + j];
                    var odd = factor * fft[start + j + half];
                    fft[start + j] = even + odd;
                    fft[start + j + half] = even - odd;
                    factor *= root;
                }
            }
        }

        var bands = new float[BandCount];
        int maxBin = FftSize / 2 - 1;
        for (int band = 0; band < BandCount; band++)
        {
            int lo = Math.Max(1, (int)Math.Pow(maxBin, band / (double)BandCount));
            int hi = Math.Max(lo + 1, (int)Math.Pow(maxBin, (band + 1) / (double)BandCount));
            hi = Math.Min(hi, maxBin);
            double energy = 0;
            int n = 0;
            for (int bin = lo; bin <= hi; bin++)
            {
                double magnitude = fft[bin].Magnitude / (FftSize / 2.0);
                energy += magnitude * magnitude;
                n++;
            }
            bands[band] = (float)Math.Clamp(Math.Sqrt(energy / Math.Max(1, n)) * 9.0, 0, 1);
        }

        for (int i = 1; i <= Math.Min(12, maxBin); i++)
            bassEnergy += fft[i].Magnitude;
        Volatile.Write(ref _bassPulse, Math.Clamp(bassEnergy / 8.0, 0, 1));
        Volatile.Write(ref _bands, bands);
    }

    private static int ReverseBits(int value, int bits)
    {
        int result = 0;
        for (int i = 0; i < bits; i++) { result = (result << 1) | (value & 1); value >>= 1; }
        return result;
    }

    public void Dispose()
    {
        try { _capture?.StopRecording(); } catch { }
        _capture?.Dispose();
    }
}