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
    private double _progress;
    private bool _targetVisible;
    private int _style;
    private double _phase;

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
        Height = 100;
        _animation.Tick += (_, _) =>
        {
            _progress += (_targetVisible ? 1 : -1) * 0.12;
            _progress = Math.Clamp(_progress, 0, 1);
            _phase += 0.2;
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
        Width = _edge is ScreenEdge.Top or ScreenEdge.Bottom ? SystemParameters.PrimaryScreenWidth : 100;
        Height = _edge is ScreenEdge.Top or ScreenEdge.Bottom ? 100 : SystemParameters.PrimaryScreenHeight;
        UpdatePosition();
    }

    public void SetLevel(double level, int style)
    {
        _level = Math.Clamp(level, 0, 1);
        _style = Math.Clamp(style, 0, 2);
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
        int count = horizontal ? 64 : 42;
        double span = horizontal ? w : h;
        double thickness = 2 + _progress * 3;
        var glow = new SolidColorBrush(Color.FromArgb((byte)(90 * _progress), _color.R, _color.G, _color.B));
        var solid = new SolidColorBrush(Color.FromArgb((byte)(240 * _progress), _color.R, _color.G, _color.B));
        dc.DrawRectangle(new SolidColorBrush(Color.FromArgb((byte)(18 * _progress), 10, 15, 25)), null, new Rect(0, 0, w, h));
        for (int i = 0; i < count; i++)
        {
            double x = horizontal ? i * span / count : 10 + (Math.Sin(_phase + i * .28) * .5 + .5) * (w - 20);
            double amplitude = (0.12 + _level * .78) * (0.25 + Math.Abs(Math.Sin(_phase * (1.2 + i % 5 * .08) + i * .31)) * .75);
            if (_style == 1) amplitude = (0.15 + _level * .8) * (0.5 + 0.5 * Math.Sin(_phase + i * .18));
            if (_style == 2) amplitude = (0.08 + _level * .9) * (0.2 + Math.Abs(Math.Sin(i * .6 + _phase * 1.6)));
            double bar = Math.Max(3, (horizontal ? h : w) * amplitude * .8);
            Rect r = horizontal
                ? new Rect(x, (h - bar) / 2, Math.Max(2, span / count * .55), bar)
                : new Rect((w - bar) / 2, i * span / count, bar, Math.Max(2, span / count * .55));
            dc.DrawRoundedRectangle(glow, null, new Rect(r.X - 2, r.Y - 2, r.Width + 4, r.Height + 4), 3, 3);
            dc.DrawRoundedRectangle(solid, null, r, 2, 2);
        }
    }
}