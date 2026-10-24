using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace ScarlettMonitorOSD;

public static class Fc2Manager
{
    private const string DefaultFc2Path = @"C:\Program Files\Focusrite\Focusrite Control 2\Focusrite Control 2.exe";

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    private const int SW_RESTORE = 9;

    private static IntPtr _lastFc2Hwnd = IntPtr.Zero;

    public static string GetFc2Path()
    {
        if (File.Exists(DefaultFc2Path))
            return DefaultFc2Path;

        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string candidate = Path.Combine(localAppData, @"Programs\Focusrite Control 2\Focusrite Control 2.exe");
        if (File.Exists(candidate))
            return candidate;

        return DefaultFc2Path;
    }

    public static bool IsRunning()
    {
        try
        {
            return Process.GetProcessesByName("Focusrite Control 2").Length > 0;
        }
        catch
        {
            return false;
        }
    }

    public static IntPtr FindFc2Window()
    {
        if (_lastFc2Hwnd != IntPtr.Zero && IsWindow(_lastFc2Hwnd))
        {
            return _lastFc2Hwnd;
        }

        IntPtr found = IntPtr.Zero;
        EnumWindows((hWnd, lp) =>
        {
            var sb = new StringBuilder(256);
            GetWindowText(hWnd, sb, 256);
            var cls = new StringBuilder(256);
            GetClassName(hWnd, cls, 256);
            string title = sb.ToString();
            string clsn = cls.ToString();

            if (title == "Focusrite Control 2" && clsn.StartsWith("JUCE"))
            {
                found = hWnd;
                return false;
            }

            // Fallback: check process name if title matches
            if (title == "Focusrite Control 2")
            {
                GetWindowThreadProcessId(hWnd, out uint pid);
                try
                {
                    var proc = Process.GetProcessById((int)pid);
                    if (proc.ProcessName.Equals("Focusrite Control 2", StringComparison.OrdinalIgnoreCase))
                    {
                        found = hWnd;
                        return false;
                    }
                }
                catch {}
            }

            return true;
        }, IntPtr.Zero);

        _lastFc2Hwnd = found;
        return found;
    }

    public static bool IsFc2Visible()
    {
        IntPtr hwnd = FindFc2Window();
        if (hwnd == IntPtr.Zero || !IsWindow(hwnd)) return false;
        return IsWindowVisible(hwnd);
    }

    public static void EnsureRunning()
    {
        if (!IsRunning())
        {
            LaunchFc2Gui();
        }
    }

    public static void LaunchFc2Gui()
    {
        try
        {
            string path = GetFc2Path();
            if (!File.Exists(path)) return;

            var psi = new ProcessStartInfo
            {
                FileName = path,
                WorkingDirectory = Path.GetDirectoryName(path)!,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch {}
    }

    public static void HideFc2()
    {
        try
        {
            IntPtr hwnd = FindFc2Window();
            if (hwnd != IntPtr.Zero && IsWindow(hwnd))
            {
                ShowWindow(hwnd, SW_HIDE);
            }
        }
        catch {}
    }

    public static void ShowFc2()
    {
        try
        {
            IntPtr hwnd = FindFc2Window();
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd))
            {
                LaunchFc2Gui();
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    for (int i = 0; i < 30; i++)
                    {
                        Thread.Sleep(200);
                        IntPtr w = FindFc2Window();
                        if (w != IntPtr.Zero && IsWindow(w))
                        {
                            ShowWindow(w, SW_RESTORE);
                            SetForegroundWindow(w);
                            break;
                        }
                    }
                });
                return;
            }

            ShowWindow(hwnd, SW_RESTORE);
            SetForegroundWindow(hwnd);
        }
        catch {}
    }

    public static void ToggleFc2()
    {
        if (IsFc2Visible())
        {
            HideFc2();
        }
        else
        {
            ShowFc2();
        }
    }

    public static void RestoreOnExit()
    {
        try
        {
            IntPtr hwnd = FindFc2Window();
            if (hwnd != IntPtr.Zero && IsWindow(hwnd) && !IsWindowVisible(hwnd))
            {
                ShowWindow(hwnd, SW_RESTORE);
                SetForegroundWindow(hwnd);
            }
        }
        catch {}
    }

    public static void KillFc2()
    {
        try
        {
            var processes = Process.GetProcessesByName("Focusrite Control 2");
            foreach (var p in processes)
            {
                try
                {
                    p.Kill();
                }
                catch {}
            }
        }
        catch {}
    }
}
