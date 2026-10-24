using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ScarlettMonitorOSD;

public class TrayMouseWheelHook : IDisposable
{
    private const int WH_MOUSE_LL = 14;
    private const int WM_MOUSEWHEEL = 0x020A;
    private const uint GA_ROOT = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(POINT Point);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

    private readonly LowLevelMouseProc _proc;
    private IntPtr _hookId = IntPtr.Zero;
    private readonly Action<int> _onWheel;

    public TrayMouseWheelHook(Action<int> onWheel)
    {
        _onWheel = onWheel;
        _proc = HookCallback;
        _hookId = SetHook(_proc);
    }

    private IntPtr SetHook(LowLevelMouseProc proc)
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule?.ModuleName), 0);
    }

    private static bool IsCursorStrictlyOverTray(POINT pt)
    {
        try
        {
            IntPtr hWnd = WindowFromPoint(pt);
            if (hWnd == IntPtr.Zero) return false;

            var sb = new StringBuilder(256);
            GetClassName(hWnd, sb, 256);
            string cls = sb.ToString();

            // 1. Direct hit on taskbar tray notification area or overflow window
            if (cls == "TrayNotifyWnd" || cls == "NotifyIconOverflowWindow")
            {
                return true;
            }

            // 2. Toolbar containing tray icons inside Shell_TrayWnd or NotifyIconOverflowWindow
            if (cls == "ToolbarWindow32")
            {
                IntPtr root = GetAncestor(hWnd, GA_ROOT);
                if (root != IntPtr.Zero)
                {
                    var rootSb = new StringBuilder(256);
                    GetClassName(root, rootSb, 256);
                    string rootCls = rootSb.ToString();
                    if (rootCls == "Shell_TrayWnd" || rootCls == "NotifyIconOverflowWindow")
                    {
                        return true;
                    }
                }
            }
        }
        catch {}

        return false;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_MOUSEWHEEL)
        {
            MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            // Strictly check real-time cursor position under tray window
            if (IsCursorStrictlyOverTray(hookStruct.pt))
            {
                int delta = (short)((hookStruct.mouseData >> 16) & 0xffff);
                _onWheel?.Invoke(delta > 0 ? 1 : -1);
                return (IntPtr)1; // Consume message so taskbar doesn't scroll
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }
}
