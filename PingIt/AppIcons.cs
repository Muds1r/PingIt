namespace PingIt;

internal static class AppIcons
{
    private static Icon? _cached;

    public static Icon Tray
    {
        get
        {
            if (_cached is not null)
                return _cached;

            var path = Path.Combine(AppContext.BaseDirectory, "app.ico");
            if (File.Exists(path))
            {
                try
                {
                    _cached = new Icon(path);
                    return _cached;
                }
                catch
                {
                    // Fall back to the executable icon if app.ico is missing or invalid.
                }
            }

            _cached = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
            return _cached;
        }
    }
}
