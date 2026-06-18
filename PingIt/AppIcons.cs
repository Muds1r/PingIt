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
                _cached = new Icon(path);
                return _cached;
            }

            _cached = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
            return _cached;
        }
    }
}
