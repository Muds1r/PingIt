using System.Runtime.InteropServices;

namespace PingIt;

internal static class Win32Window
{
    private static readonly IntPtr HwndTopMost = new(-1);

    private const int GwlExstyle = -20;
    private const int WsExTransparent = 0x00000020;

    private const uint SwpNomove = 0x0002;
    private const uint SwpNosize = 0x0001;
    private const uint SwpNoactivate = 0x0010;
    private const uint SwpShowwindow = 0x0040;

    [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    public static void EnsureTopMost(IntPtr handle) =>
        SetWindowPos(handle, HwndTopMost, 0, 0, 0, 0, SwpNomove | SwpNosize | SwpNoactivate | SwpShowwindow);

    public static void SetClickThrough(IntPtr handle, bool enabled)
    {
        if (handle == IntPtr.Zero)
            return;

        var style = GetWindowLongPtr(handle, GwlExstyle).ToInt64();
        var updated = enabled
            ? style | WsExTransparent
            : style & ~WsExTransparent;

        SetWindowLongPtr(handle, GwlExstyle, new IntPtr(updated));
    }

    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex) =>
        IntPtr.Size == 8
            ? GetWindowLongPtr64(hWnd, nIndex)
            : new IntPtr(GetWindowLong32(hWnd, nIndex));

    private static void SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr newValue)
    {
        if (IntPtr.Size == 8)
            SetWindowLongPtr64(hWnd, nIndex, newValue);
        else
            SetWindowLong32(hWnd, nIndex, newValue.ToInt32());
    }
}
