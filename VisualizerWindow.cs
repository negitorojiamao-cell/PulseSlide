using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace PulseSlide;

public enum ScreenEdge { Top, Bottom, Left, Right }

public sealed class VisualizerWindow : Window
{
    private readonly DispatcherTimer _animation = new() { Interval = TimeSpan.FromMilliseconds(35) };
    private ScreenEdge _edge = ScreenEdge.Top;
    private Color _color = Color.FromRgb(53, 230, 255);
    private double _level;
    private double _bassPulse;
    private double _progress;
    private bool _targetVisible;
    private bool _testing;
    private int _style;
    private double _phase;
    private float[] _waveform = Array.Empty<float>();
    private float[] _bands = Array.Empty<float>();
    private double _sensitivity = 3;

    public VisualizerWindow()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        Topmost = true;
        ResizeMode = ResizeMode.NoResize;
        IsHitTestVisible = false;
        Width = SystemParameters.PrimaryScreenWidth;
        Height = 112;
        _animation.Tick += (_, _) =>
        {
            _progress += (_targetVisible ? 1 : -1) * 0.12;
            _progress = Math.Clamp(_progress, 0, 1);
            _phase += 0.16;
            UpdatePosition();
            InvalidateVisual();
            if (_progress == 0 && !_targetVisible) Hide();
        };
        _animation.Start();
    }

    public void Configure(ScreenEdge edge, Color color)
    {
        _edge = edge;
        _color = color;
        Width = _edge is ScreenEdge.Top or ScreenEdge.Bottom ? SystemParameters.PrimaryScreenWidth : 112;
        Height = _edge is ScreenEdge.Top or ScreenEdge.Bottom ? 112 : SystemParameters.PrimaryScreenHeight;
        UpdatePosition();
    }

    public void SetAudioData(double level, float[] waveform, float[] bands, double bassPulse, bool testing, int style, double sensitivity)
    {
        _level = Math.Clamp(level * Math.Max(1, sensitivity) * 12, 0, 1);
        _waveform = waveform;
        _bands = bands;
        _bassPulse = Math.Clamp(bassPulse, 0, 1);
        _testing = testing;
        _style = Math.Clamp(style, 0, 5);
        _sensitivity = sensitivity;
    }

    public void SetVisible(bool visible) => _targetVisible = visible;

    private void UpdatePosition()
    {
        double w = SystemParameters.PrimaryScreenWidth, h = SystemParameters.PrimaryScreenHeight;
        Left = _edge switch { ScreenEdge.Left => -Width + Width * _progress, ScreenEdge.Right => w - Width * _progress, _ => 0 };
        Top = _edge switch { ScreenEdge.Top => -Height + Height * _progress, ScreenEdge.Bottom => h - Height * _progress, _ => 0 };
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (_progress <= 0) return;
        double w = ActualWidth, h = ActualHeight;
        bool horizontal = _edge is ScreenEdge.Top or ScreenEdge.Bottom;
        var glowColor = Color.FromArgb((byte)(75 * _progress), _color.R, _color.G, _color.B);
        var solidColor = Color.FromArgb((byte)(235 * _progress), _color.R, _color.G, _color.B);
        var glow = new SolidColorBrush(glowColor);
        var solid = new SolidColorBrush(solidColor);
        dc.DrawRectangle(new SolidColorBrush(Color.FromArgb((byte)(16 * _progress), 8, 12, 24)), null, new Rect(0, 0, w, h));

        switch (_style)
        {
            case 1: DrawWave(dc, w, h, horizontal, glow, solid); break;
            case 2: DrawSpectrum(dc, w, h, horizontal, glow, solid); break;
            case 3: DrawCircular(dc, w, h, glow, solid); break;
            case 4: DrawVuMeter(dc, w, h, horizontal, glow, solid); break;
            case 5: DrawBeatPulse(dc, w, h, horizontal, glow, solid); break;
            default: DrawNeonBars(dc, w, h, horizontal, glow, solid); break;
        }
    }

    private void DrawNeonBars(DrawingContext dc, double w, double h, bool horizontal, Brush glow, Brush solid)
    {
        int count = horizontal ? 64 : 42;
        double span = horizontal ? w : h;
        for (int i = 0; i < count; i++)
        {
            double value = Band(i, count);
            double amplitude = 0.08 + value * 0.92;
            double bar = Math.Max(3, (horizontal ? h : w) * amplitude * 0.82);
            Rect r = horizontal
                ? new Rect(i * span / count, (h - bar) / 2, Math.Max(2, span / count * 0.56), bar)
                : new Rect((w - bar) / 2, i * span / count, bar, Math.Max(2, span / count * 0.56));
            DrawGlowingBar(dc, r, glow, solid);
        }
    }

    private void DrawSpectrum(DrawingContext dc, double w, double h, bool horizontal, Brush glow, Brush solid)
    {
        int count = horizontal ? 64 : 42;
        double span = horizontal ? w : h;
        for (int i = 0; i < count; i++)
        {
            double value = Band(i, count);
            double bar = Math.Max(2, (horizontal ? h : w) * value * 0.95);
            Rect r = horizontal
                ? new Rect(i * span / count, h - bar, Math.Max(2, span / count * 0.62), bar)
                : new Rect(0, i * span / count, bar, Math.Max(2, span / count * 0.62));
            DrawGlowingBar(dc, r, glow, solid);
        }
    }

    private void DrawWave(DrawingContext dc, double w, double h, bool horizontal, Brush glow, Brush solid)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            int count = 180;
            for (int i = 0; i < count; i++)
            {
                double t = i / (double)(count - 1);
                double sample;
                if (_testing || _waveform.Length == 0)
                    sample = Math.Sin(t * Math.PI * 12 + _phase) * (0.25 + _level * 0.6);
                else
                    sample = _waveform[(int)(t * (_waveform.Length - 1))] * Math.Max(1, _sensitivity) * 4;
                sample = Math.Clamp(sample, -1, 1);
                Point p = horizontal
                    ? new Point(t * w, h / 2 - sample * h * 0.43)
                    : new Point(w / 2 + sample * w * 0.43, t * h);
                if (i == 0) ctx.BeginFigure(p, false, false); else ctx.LineTo(p, true, false);
            }
        }
        geometry.Freeze();
        dc.DrawGeometry(null, new Pen(glow, 7), geometry);
        dc.DrawGeometry(null, new Pen(solid, 2), geometry);
    }

    private void DrawCircular(DrawingContext dc, double w, double h, Brush glow, Brush solid)
    {
        Point center = new(w / 2, h / 2);
        double radius = Math.Min(w, h) * 0.22;
        dc.DrawEllipse(null, new Pen(glow, 9), center, radius, radius);
        dc.DrawEllipse(null, new Pen(solid, 2), center, radius, radius);
        int count = 72;
        for (int i = 0; i < count; i++)
        {
            double angle = 2 * Math.PI * i / count - Math.PI / 2;
            double value = Band(i, count);
            double outer = radius + 6 + value * Math.Min(w, h) * 0.22;
            Point p1 = new(center.X + Math.Cos(angle) * (radius + 3), center.Y + Math.Sin(angle) * (radius + 3));
            Point p2 = new(center.X + Math.Cos(angle) * outer, center.Y + Math.Sin(angle) * outer);
            dc.DrawLine(new Pen(glow, 5), p1, p2);
            dc.DrawLine(new Pen(solid, 2), p1, p2);
        }
        dc.DrawEllipse(solid, null, center, 3 + _level * 5, 3 + _level * 5);
    }

    private void DrawVuMeter(DrawingContext dc, double w, double h, bool horizontal, Brush glow, Brush solid)
    {
        double faceW = horizontal ? Math.Min(w * 0.42, 420) : w * 0.82;
        double faceH = horizontal ? h * 0.72 : Math.Min(h * 0.42, 420);
        Rect face = new((w - faceW) / 2, (h - faceH) / 2, faceW, faceH);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb((byte)(50 * _progress), 5, 8, 15)), new Pen(glow, 2), face, 12, 12);
        Point pivot = new(face.X + face.Width / 2, face.Y + face.Height * 0.78);
        double radius = Math.Min(face.Width * 0.42, face.Height * 0.7);
        double start = Math.PI * 1.15, end = Math.PI * 1.85;
        for (int i = 0; i <= 30; i++)
        {
            double a = start + (end - start) * i / 30.0;
            Point p1 = new(pivot.X + Math.Cos(a) * (radius - 7), pivot.Y + Math.Sin(a) * (radius - 7));
            Point p2 = new(pivot.X + Math.Cos(a) * radius, pivot.Y + Math.Sin(a) * radius);
            Brush tick = i > 24 ? Brushes.OrangeRed : solid;
            dc.DrawLine(new Pen(tick, i % 5 == 0 ? 2 : 1), p1, p2);
        }
        double needleAngle = start + (end - start) * Math.Clamp(_level, 0, 1);
        Point tip = new(pivot.X + Math.Cos(needleAngle) * (radius - 12), pivot.Y + Math.Sin(needleAngle) * (radius - 12));
        dc.DrawLine(new Pen(glow, 7), pivot, tip);
        dc.DrawLine(new Pen(solid, 2), pivot, tip);
        dc.DrawEllipse(solid, null, pivot, 4, 4);
    }

    private void DrawBeatPulse(DrawingContext dc, double w, double h, bool horizontal, Brush glow, Brush solid)
    {
        double pulse = Math.Clamp(_bassPulse * 1.8 + _level * 0.2, 0, 1);
        double cx = w / 2, cy = h / 2;
        double maxRadius = Math.Min(w, h) * 0.43;
        for (int i = 5; i >= 1; i--)
        {
            double r = maxRadius * (i / 5.0) * (0.45 + pulse * 0.65);
            byte alpha = (byte)(12 * _progress + pulse * 20);
            var brush = new SolidColorBrush(Color.FromArgb(alpha, _color.R, _color.G, _color.B));
            dc.DrawEllipse(brush, new Pen(glow, 2 + pulse * 3), new Point(cx, cy), r, r);
        }
        int particles = 42;
        for (int i = 0; i < particles; i++)
        {
            double angle = 2 * Math.PI * i / particles + _phase * (0.4 + pulse);
            double jitter = 0.65 + 0.35 * Math.Sin(_phase * 1.7 + i * 4.13);
            double r = maxRadius * (0.5 + pulse * 0.48) * jitter;
            Point p = new(cx + Math.Cos(angle) * r, cy + Math.Sin(angle) * r);
            double size = 1.5 + pulse * 3.5;
            dc.DrawEllipse(glow, null, p, size * 2.4, size * 2.4);
            dc.DrawEllipse(solid, null, p, size, size);
        }
        if (pulse > 0.48)
            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb((byte)((pulse - 0.48) * 22 * _progress), _color.R, _color.G, _color.B)), null, new Rect(0, 0, w, h));
    }

    private double Band(int index, int count)
    {
        if (_testing || _bands.Length == 0)
            return Math.Clamp(0.1 + 0.75 * Math.Abs(Math.Sin(index * 0.47 + _phase * (1 + index % 4 * 0.12))) * _level, 0, 1);
        int bandIndex = Math.Clamp((int)(index / (double)count * _bands.Length), 0, _bands.Length - 1);
        return Math.Clamp(_bands[bandIndex] * Math.Max(1, _sensitivity) * 2.4, 0, 1);
    }

    private static void DrawGlowingBar(DrawingContext dc, Rect r, Brush glow, Brush solid)
    {
        dc.DrawRoundedRectangle(glow, null, new Rect(r.X - 2, r.Y - 2, r.Width + 4, r.Height + 4), 3, 3);
        dc.DrawRoundedRectangle(solid, null, r, 2, 2);
    }
}