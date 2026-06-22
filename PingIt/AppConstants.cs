namespace PingIt;

internal static class AppConstants
{
    public const string AppName = "PingIt";
    public const string DefaultPingHost = "1.1.1.1";
    public const string FontFamily = "Segoe UI";

    public const int NetworkSampleIntervalMs = 1000;
    public const int PingIntervalMs = 3000;
    public const int TopMostRefreshIntervalMs = 500;
    public const int NicCacheRefreshSeconds = 30;
    public const int PingTimeoutMs = 2000;

    public static readonly string[] PingHostPresets =
    [
        "1.1.1.1",
        "8.8.8.8",
        "9.9.9.9",
        "google.com"
    ];

    public static readonly double[] OpacityPresets = [0.35, 0.50, 0.65, 0.85, 1.00];
    public static readonly (string Label, TextSize Size)[] TextSizePresets =
    [
        ("Small", TextSize.Small),
        ("Medium", TextSize.Medium),
        ("Large", TextSize.Large)
    ];

    public static readonly Color BackgroundColor = Color.FromArgb(18, 18, 22);
    public static readonly Color BorderColor = Color.FromArgb(40, 255, 255, 255);
    public static readonly Color IconColor = Color.FromArgb(130, 140, 155);
    public static readonly Color ValueColor = Color.FromArgb(235, 237, 240);
}
