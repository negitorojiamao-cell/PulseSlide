using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace PulseSlide;

public partial class MainWindow : Window
{
    private readonly AudioMonitor _audio = new();
    private readonly VisualizerWindow _visualizer = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(35) };
    private DateTime _lastLoud = DateTime.MinValue;
    private DateTime _testUntil = DateTime.MinValue;
    private double _level;
    private bool _shown;

    public MainWindow()
    {
        InitializeComponent();
        _audio.Start();
        _timer.Tick += (_, _) => UpdateAudio();
        _timer.Start();
        DelaySlider.ValueChanged += (_, _) => DelayLabel.Text = $"{DelaySlider.Value:0} 秒";
        ApplySettings();
        StatusText.Text = "● 出力音声を監視中";
        Closed += (_, _) => { _timer.Stop(); _audio.Dispose(); _visualizer.Close(); };
    }

    private void UpdateAudio()
    {
        _level = _audio.Level;
        var threshold = 0.0015 / Math.Max(1, SensitivitySlider.Value);
        if (_level > threshold) _lastLoud = DateTime.UtcNow;
        bool active = DateTime.UtcNow < _testUntil || DateTime.UtcNow - _lastLoud < TimeSpan.FromSeconds(DelaySlider.Value);
        if (active)
        {
            if (!_shown) { _visualizer.Show(); _shown = true; }
            _visualizer.SetLevel(Math.Clamp(_level * SensitivitySlider.Value * 45, 0, 1), StyleBox.SelectedIndex);
            _visualizer.SetVisible(true);
        }
        else if (_shown)
        {
            _visualizer.SetVisible(false);
        }
    }

    private void Apply_Click(object sender, RoutedEventArgs e) => ApplySettings();

    private void ApplySettings()
    {
        var edge = EdgeBox.SelectedIndex switch { 1 => ScreenEdge.Bottom, 2 => ScreenEdge.Left, 3 => ScreenEdge.Right, _ => ScreenEdge.Top };
        var color = ColorBox.SelectedIndex switch
        {
            1 => Color.FromRgb(255, 45, 180),
            2 => Color.FromRgb(120, 255, 65),
            3 => Color.FromRgb(164, 100, 255),
            4 => Colors.White,
            _ => Color.FromRgb(53, 230, 255)
        };
        _visualizer.Configure(edge, color);
        StatusText.Text = "● 設定を適用しました";
    }

    private void Test_Click(object sender, RoutedEventArgs e)
    {
        ApplySettings();
        _testUntil = DateTime.UtcNow.AddSeconds(4);
        _lastLoud = DateTime.UtcNow;
        if (!_shown) { _visualizer.Show(); _shown = true; }
        _visualizer.SetLevel(0.85, StyleBox.SelectedIndex);
        _visualizer.SetVisible(true);
    }
}