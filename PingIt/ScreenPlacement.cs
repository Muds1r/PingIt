namespace PingIt;

internal static class ScreenPlacement
{
    public static Point ClampToVisibleArea(Point location, Size size)
    {
        var screen = Screen.FromPoint(location);
        var area = screen.WorkingArea;

        var maxX = Math.Max(area.Left, area.Right - size.Width);
        var maxY = Math.Max(area.Top, area.Bottom - size.Height);

        return new Point(
            Math.Clamp(location.X, area.Left, maxX),
            Math.Clamp(location.Y, area.Top, maxY));
    }
}
