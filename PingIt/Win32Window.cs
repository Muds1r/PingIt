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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

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

        var style = GetWindowLong(handle, GwlExstyle);
        var updated = enabled
            ? style | WsExTransparent
            : style & ~WsExTransparent;

        SetWindowLong(handle, GwlExstyle, updated);
    }
}
