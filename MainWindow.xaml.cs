using System;
using System.Windows;
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
        double level = _audio.Level;
        double threshold = 0.0015 / Math.Max(1, SensitivitySlider.Value);
        if (level > threshold) _lastLoud = DateTime.UtcNow;
        bool testing = DateTime.UtcNow < _testUntil;
        bool active = testing || DateTime.UtcNow - _lastLoud < TimeSpan.FromSeconds(DelaySlider.Value);
        if (active)
        {
            if (!_shown) { _visualizer.Show(); _shown = true; }
            _visualizer.SetAudioData(
                level,
                _audio.Waveform,
                _audio.Bands,
                _audio.BassPulse,
                testing,
                StyleBox.SelectedIndex,
                SensitivitySlider.Value);
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
        _visualizer.SetAudioData(0.35, Array.Empty<float>(), Array.Empty<float>(), 0.8, true, StyleBox.SelectedIndex, SensitivitySlider.Value);
        _visualizer.SetVisible(true);
    }
}