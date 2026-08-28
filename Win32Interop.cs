using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FatCatPet;

/// <summary>
/// Win32 窗口样式互操作：从 Alt+Tab 隐藏、鼠标穿透开关。
/// </summary>
public static class Win32Interop
{
    private const int GWL_EXSTYLE = -20;

    private const long WS_EX_TRANSPARENT = 0x00000020L;  // 鼠标穿透
    private const long WS_EX_TOOLWINDOW = 0x00000080L;   // 不显示在任务栏/Alt+Tab
    private const long WS_EX_NOACTIVATE = 0x08000000L;   // 点击不抢焦点

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    private static long GetExStyle(IntPtr hwnd)
        => IntPtr.Size == 8
            ? GetWindowLongPtr64(hwnd, GWL_EXSTYLE).ToInt64()
            : GetWindowLong32(hwnd, GWL_EXSTYLE);

    private static void SetExStyle(IntPtr hwnd, long style)
    {
        if (IntPtr.Size == 8)
            SetWindowLongPtr64(hwnd, GWL_EXSTYLE, new IntPtr(style));
        else
            SetWindowLong32(hwnd, GWL_EXSTYLE, (int)style);
    }

    /// <summary>隐藏于 Alt+Tab 列表，并让窗口点击时不抢焦点。</summary>
    public static void HideFromAltTab(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        SetExStyle(hwnd, GetExStyle(hwnd) | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
    }

    /// <summary>开启/关闭鼠标穿透（穿透时鼠标事件直接落到下层窗口）。</summary>
    public static void SetClickThrough(Window window, bool enable)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        long style = GetExStyle(hwnd);
        style = enable ? style | WS_EX_TRANSPARENT : style & ~WS_EX_TRANSPARENT;
        SetExStyle(hwnd, style);
    }
}
