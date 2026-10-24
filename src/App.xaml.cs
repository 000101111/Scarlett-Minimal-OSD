using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Windows;
using Forms = System.Windows.Forms;

namespace ScarlettMonitorOSD;

public partial class App : System.Windows.Application
{
    private static Mutex? _mutex;
    private static EventWaitHandle? _showSignal;
    private static Thread? _signalWatcherThread;
    private Forms.NotifyIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private SettingsWindow? _settingsWindow;
    private AppSettings _settings = new();
    private TrayMouseWheelHook? _wheelHook;

    private const string MutexName = "Global\\ScarlettMonitorOSD_SingleInstance_Mutex";
    private const string SignalName = "Global\\ScarlettMonitorOSD_ShowSignal";

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
        {
            Log($"[CRASH] AppDomain: {ev.ExceptionObject}");
            Fc2Manager.RestoreOnExit();
        };

        DispatcherUnhandledException += (s, ev) =>
        {
            Log($"[CRASH] Dispatcher: {ev.Exception}");
            ev.Handled = true;
        };

        bool createdNew;
        _mutex = new Mutex(true, MutexName, out createdNew);

        if (!createdNew)
        {
            Log("[Startup] Another instance already running. Signaling it to show OSD...");
            try
            {
                using var signal = EventWaitHandle.OpenExisting(SignalName);
                signal.Set();
            }
            catch (Exception ex)
            {
                Log($"[Startup] Could not signal existing instance: {ex.Message}");
            }

            Shutdown();
            return;
        }

        // Create the event wait handle for future instances to signal us
        _showSignal = new EventWaitHandle(false, EventResetMode.AutoReset, SignalName);
        _signalWatcherThread = new Thread(WatchSignal) { IsBackground = true };
        _signalWatcherThread.Start();

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Load configuration
        _settings = AppSettings.Load();
        Localization.SetLanguage(_settings.Language);

        // Check and auto-launch FC2 if enabled
        if (_settings.AutoLaunchFc2)
        {
            Log("[Startup] Auto-launching Focusrite Control 2...");
            Fc2Manager.EnsureRunning();
        }

        _mainWindow = new MainWindow();
        MainWindow = _mainWindow;
        _mainWindow.ApplySettings(_settings);
        _mainWindow.Show();

        // Initialize tray icon
        InitTrayIcon();
    }

    private void WatchSignal()
    {
        while (_showSignal != null)
        {
            try
            {
                if (_showSignal.WaitOne())
                {
                    Dispatcher.Invoke(() =>
                    {
                        Log("[Signal] Show signal received from another instance.");
                        _mainWindow?.TriggerOSD();
                    });
                }
            }
            catch (ThreadAbortException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log($"[Signal] Watch error: {ex.Message}");
                Thread.Sleep(500);
            }
        }
    }

    private void InitTrayIcon()
    {
        try
        {
            _trayIcon = new Forms.NotifyIcon
            {
                Text = "Focusrite Scarlett Monitor OSD",
                Icon = CreateAppIcon(),
                Visible = true
            };

            var contextMenu = new Forms.ContextMenuStrip();

            // 1. 显示/隐藏 Focusrite Control 2
            var toggleFc2Item = new Forms.ToolStripMenuItem(Localization.Text("ToggleFc2"));
            toggleFc2Item.Click += (s, e) => Fc2Manager.ToggleFc2();

            // 2. OSD 设置
            var settingsItem = new Forms.ToolStripMenuItem(Localization.Text("TraySettings"));
            settingsItem.Click += (s, e) => ShowSettingsWindow();

            // 3. 退出
            var exitItem = new Forms.ToolStripMenuItem(Localization.Text("Exit"));
            exitItem.Click += (s, e) => Shutdown();

            contextMenu.Opening += (s, e) =>
            {
                toggleFc2Item.Text = Fc2Manager.IsFc2Visible()
                    ? Localization.Text("HideFc2")
                    : Localization.Text("ShowFc2");
                settingsItem.Text = Localization.Text("TraySettings");
                exitItem.Text = Localization.Text("Exit");
            };

            contextMenu.Items.Add(toggleFc2Item);
            contextMenu.Items.Add(settingsItem);
            contextMenu.Items.Add(new Forms.ToolStripSeparator());
            contextMenu.Items.Add(exitItem);

            _trayIcon.ContextMenuStrip = contextMenu;

            // Double click: Toggle FC2 visibility (Show -> Hide -> Show)
            _trayIcon.DoubleClick += (s, e) => Fc2Manager.ToggleFc2();

            // Taskbar tray mouse wheel to adjust monitor volume (strictly over tray area)
            _wheelHook = new TrayMouseWheelHook((direction) =>
            {
                Dispatcher.Invoke(() =>
                {
                    double step = _settings.VolumeStepDb > 0 ? _settings.VolumeStepDb : 1.0;
                    _mainWindow?.ChangeGain(direction > 0 ? step : -step);
                });
            });
        }
        catch (Exception ex)
        {
            Log($"[Tray] Init exception: {ex.Message}");
        }
    }

    public void ShowSettingsWindow()
    {
        if (_mainWindow == null) return;

        if (_settingsWindow != null && _settingsWindow.IsLoaded)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_mainWindow, _settings);
        _settingsWindow.Closed += (s, e) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private static Icon CreateAppIcon()
    {
        try
        {
            string fc2Path = Fc2Manager.GetFc2Path();
            if (File.Exists(fc2Path))
            {
                var icon = Icon.ExtractAssociatedIcon(fc2Path);
                if (icon != null) return icon;
            }
        }
        catch {}

        try
        {
            string localIco = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(localIco))
            {
                return new Icon(localIco);
            }
        }
        catch {}

        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(System.Drawing.Color.FromArgb(236, 62, 76));
            g.FillEllipse(brush, 2, 2, 28, 28);
            using var font = new System.Drawing.Font("Segoe UI", 13, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
            using var textBrush = new SolidBrush(System.Drawing.Color.White);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("S", font, textBrush, new RectangleF(0, 0, 32, 32), sf);
        }
        return Icon.FromHandle(bmp.GetHicon());
    }

    private static void Log(string msg)
    {
        try
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "osd.log");
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n");
        }
        catch {}
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        Log($"[App Exit] ExitCode: {e.ApplicationExitCode}");

        if (_settings.KillFc2OnExit)
        {
            Log("[App Exit] KillFc2OnExit is enabled. Stopping Focusrite Control 2...");
            Fc2Manager.KillFc2();
        }
        else
        {
            // Restore FC2 to visible state if it was hidden
            Fc2Manager.RestoreOnExit();
        }

        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        _wheelHook?.Dispose();
        _wheelHook = null;

        try
        {
            _mutex?.ReleaseMutex();
        }
        catch {}
        _mutex?.Dispose();
        _showSignal?.Dispose();
    }
}
