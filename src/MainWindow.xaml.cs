using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Color = System.Windows.Media.Color;

namespace ScarlettMonitorOSD;

public partial class MainWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const long WS_EX_TOOLWINDOW = 0x00000080L;
    private const long WS_EX_NOACTIVATE = 0x08000000L;
    private const long WS_EX_TRANSPARENT = 0x00000020L;

    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    private static readonly SolidColorBrush ActiveBrush = new(Color.FromRgb(236, 62, 76)); // Focusrite Red
    private static readonly SolidColorBrush InactiveBrush = new(Color.FromRgb(119, 119, 119));

    private readonly DispatcherTimer _hideTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private Process? _bridgeProcess;
    private bool _lastConnected;
    private double? _lastGain;
    private string? _lastGroup;
    private bool _lastMute;
    private bool _lastDim;
    private bool _firstState = true;
    private AppSettings _settings = new();
    private bool _isPreviewMode;

    public MainWindow()
    {
        InitializeComponent();
        ActiveBrush.Freeze();
        InactiveBrush.Freeze();
        _hideTimer.Tick += (_, _) => { _hideTimer.Stop(); HideOsd(); };
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        PositionAtRightCenter();
        SetupWindowStyles();
        Visibility = Visibility.Hidden;
        SetDisplay(false, null, null, false, false);
        StartBridge();
        TriggerOSD(TimeSpan.FromSeconds(2.0));
    }

    private void Window_Closed(object? sender, EventArgs e) => StopBridge();

    private void PositionAtRightCenter()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 30;
        Top = workArea.Top + (workArea.Height - Height) / 2;
    }

    private void SetupWindowStyles()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;
        long current = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
        long updated = (current | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE) & ~WS_EX_TRANSPARENT;
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(updated));
    }

    private void SetDisplay(bool connected, double? gain, string? group, bool mute, bool dim)
    {
        GainValueText.Text = connected && gain is double value && double.IsFinite(value)
            ? value.ToString("0.0", CultureInfo.InvariantCulture)
            : "--.-";

        bool main = connected && string.Equals(group, "MAIN", StringComparison.OrdinalIgnoreCase);
        bool alt = connected && string.Equals(group, "ALT", StringComparison.OrdinalIgnoreCase);
        MainText.Foreground = main ? ActiveBrush : InactiveBrush;
        AltText.Foreground = alt ? ActiveBrush : InactiveBrush;

        // Mute / Dim indicators are only visible when active
        MuteText.Visibility = (connected && mute) ? Visibility.Visible : Visibility.Collapsed;
        DimText.Visibility = (connected && dim) ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateState(bool connected, double? gain, string? group, bool mute, bool dim)
    {
        bool changed = _firstState ||
            _lastConnected != connected ||
            _lastGain != gain ||
            !string.Equals(_lastGroup, group, StringComparison.OrdinalIgnoreCase) ||
            _lastMute != mute ||
            _lastDim != dim;

        _firstState = false;
        _lastConnected = connected;
        _lastGain = gain;
        _lastGroup = group;
        _lastMute = mute;
        _lastDim = dim;

        SetDisplay(connected, gain, group, mute, dim);
        if (changed) TriggerOSD();
    }

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;
        ApplyOpacity(settings.BackgroundOpacity);
    }

    public void ApplyOpacity(double opacity)
    {
        byte alpha = (byte)Math.Clamp((int)(opacity * 255), 0, 255);
        RootBorder.Background = new SolidColorBrush(Color.FromArgb(alpha, 7, 7, 7));
    }

    public void SetPreviewMode(bool preview)
    {
        _isPreviewMode = preview;
        if (preview)
        {
            PositionAtRightCenter();
            Visibility = Visibility.Visible;
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
                SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);

            _hideTimer.Stop();
            BeginAnimation(OpacityProperty, null);
            Opacity = 1.0;
        }
        else
        {
            TriggerOSD(TimeSpan.FromSeconds(_settings.DisplayDurationSeconds));
        }
    }

    public void TriggerOSD(TimeSpan? duration = null)
    {
        if (_isPreviewMode) return;

        PositionAtRightCenter();
        Visibility = Visibility.Visible;
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);

        _hideTimer.Stop();
        _hideTimer.Interval = duration ?? TimeSpan.FromSeconds(_settings.DisplayDurationSeconds);
        BeginAnimation(OpacityProperty, null);
        Opacity = 1.0;
        _hideTimer.Start();
    }

    private void HideOsd()
    {
        if (_isPreviewMode) return;

        var anim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(250))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        anim.Completed += (_, _) =>
        {
            if (Opacity == 0 && !_isPreviewMode)
            {
                Visibility = Visibility.Hidden;
            }
        };
        BeginAnimation(OpacityProperty, anim);
    }

    private void RootBorder_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
        {
            // Click OSD to summon FC2 GUI
            Fc2Manager.ShowFc2();
            HideOsd();
        }
    }

    private void RootBorder_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isPreviewMode)
        {
            _hideTimer.Stop();
        }
    }

    private void RootBorder_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isPreviewMode && Visibility == Visibility.Visible)
        {
            _hideTimer.Interval = TimeSpan.FromSeconds(1.0);
            _hideTimer.Start();
        }
    }

    public void SendBridgeCommand(object cmdObj)
    {
        try
        {
            if (_bridgeProcess != null && !_bridgeProcess.HasExited)
            {
                string json = JsonSerializer.Serialize(cmdObj);
                _bridgeProcess.StandardInput.WriteLine(json);
                _bridgeProcess.StandardInput.Flush();
            }
        }
        catch (Exception ex)
        {
            Log("Bridge send error: " + ex.Message);
        }
    }

    public void ChangeGain(double delta)
    {
        // Reject writes when disconnected or unknown
        if (!_lastConnected || _lastGain == null)
            return;

        double cur = _lastGain.Value;
        double target = cur + delta;

        // Apply configurable max gain safety limit
        if (target > _settings.MaxGainLimit) target = _settings.MaxGainLimit;
        if (target < -127.0) target = -127.0;
        target = Math.Round(target, 1);

        SendBridgeCommand(new { cmd = "set_gain", value = target });
        TriggerOSD();
    }

    public void SetGroup(string group)
    {
        if (!_lastConnected) return;
        SendBridgeCommand(new { cmd = "set_group", group = group });
        TriggerOSD();
    }

    public void ToggleGroup()
    {
        if (!_lastConnected) return;
        SendBridgeCommand(new { cmd = "toggle_group" });
        TriggerOSD();
    }

    private void MainText_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
        {
            SetGroup("MAIN");
            e.Handled = true;
        }
    }

    private void AltText_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
        {
            SetGroup("ALT");
            e.Handled = true;
        }
    }

    private void OnBridgeLine(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Data)) return;
        try
        {
            using var doc = JsonDocument.Parse(e.Data);
            var root = doc.RootElement;
            if (!root.TryGetProperty("type", out var t)) return;

            string typeStr = t.GetString() ?? "";
            if (typeStr == "state")
            {
                bool connected = root.TryGetProperty("connected", out var c) && c.ValueKind == JsonValueKind.True;
                double? gain = root.TryGetProperty("gain", out var g) && g.ValueKind == JsonValueKind.Number
                    ? g.GetDouble() : null;
                string? group = root.TryGetProperty("group", out var groupValue) &&
                                groupValue.ValueKind == JsonValueKind.String ? groupValue.GetString() : null;
                bool isMute = root.TryGetProperty("mute", out var m) && m.ValueKind == JsonValueKind.True;
                bool isDim = root.TryGetProperty("dim", out var d) && d.ValueKind == JsonValueKind.True;

                Dispatcher.BeginInvoke(new Action(() => UpdateState(connected, gain, group, isMute, isDim)));
            }
            else if (typeStr == "command_error")
            {
                string errMsg = root.TryGetProperty("error", out var err) ? err.GetString() ?? "Unknown error" : "Unknown error";
                Log("Bridge reported command error: " + errMsg);
            }
        }
        catch (Exception ex) { Log("Bad bridge state: " + ex.Message); }
    }

    public void RestartBridge()
    {
        StopBridge();
        StartBridge();
        TriggerOSD(TimeSpan.FromSeconds(3));
    }

    private void StartBridge()
    {
        string node = FindNode();
        string bridge = Path.Combine(AppContext.BaseDirectory, "bridge", "bridge.mjs");
        if (!File.Exists(node) || !File.Exists(bridge))
        {
            Log("Missing Node or bridge: " + node + " / " + bridge);
            return;
        }

        try
        {
            _bridgeProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = node,
                    Arguments = "\"" + bridge + "\"",
                    WorkingDirectory = Path.GetDirectoryName(bridge)!,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };
            _bridgeProcess.OutputDataReceived += OnBridgeLine;
            _bridgeProcess.ErrorDataReceived += (_, ev) =>
            {
                if (!string.IsNullOrWhiteSpace(ev.Data)) Log("Bridge: " + ev.Data);
            };
            _bridgeProcess.Exited += (_, _) =>
            {
                Log("Bridge exited");
                if (!Dispatcher.HasShutdownStarted && !Dispatcher.HasShutdownFinished)
                    Dispatcher.BeginInvoke(new Action(() => UpdateState(false, null, null, false, false)));
            };
            _bridgeProcess.Start();
            _bridgeProcess.BeginOutputReadLine();
            _bridgeProcess.BeginErrorReadLine();
        }
        catch (Exception ex) { Log("StartBridge failed: " + ex.Message); }
    }

    private void StopBridge()
    {
        try
        {
            if (_bridgeProcess == null) return;
            if (!_bridgeProcess.HasExited) _bridgeProcess.Kill(entireProcessTree: true);
            _bridgeProcess.Dispose();
            _bridgeProcess = null;
        }
        catch (Exception ex) { Log("StopBridge failed: " + ex.Message); }
    }

    public static string FindNode()
    {
        // 1. Standalone local node.exe in base directory (portable package)
        string local = Path.Combine(AppContext.BaseDirectory, "node.exe");
        if (File.Exists(local)) return local;

        // 2. Program Files nodejs
        string pf = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "nodejs", "node.exe");
        if (File.Exists(pf)) return pf;

        // 3. User winget package
        string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string winget = Path.Combine(user,
            @"AppData\Local\Microsoft\WinGet\Packages\OpenJS.NodeJS.LTS_Microsoft.Winget.Source_8wekyb3d8bbwe\node-v24.20.0-win-x64\node.exe");
        if (File.Exists(winget)) return winget;

        // 4. PATH lookup
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (string directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate = Path.Combine(directory.Trim('"'), "node.exe");
            if (File.Exists(candidate)) return candidate;
        }
        return "node.exe";
    }

    private static void Log(string message)
    {
        try
        {
            File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "osd.log"),
                $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
        }
        catch { }
    }
}
