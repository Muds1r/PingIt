using System.Text.Json;

namespace PingIt;

internal sealed class AppSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public int X { get; set; } = 20;
    public int Y { get; set; } = 20;
    public string PingHost { get; set; } = AppConstants.DefaultPingHost;
    public TextSize TextSize { get; set; } = TextSize.Medium;
    public double Opacity { get; set; } = 0.85;
    public bool ShowDownload { get; set; } = true;
    public bool ShowUpload { get; set; } = true;
    public bool ShowPing { get; set; } = true;
    public bool StartWithWindows { get; set; }

    public bool SetupCompleted { get; set; }

    public bool NeedsNetworkSampling => ShowDownload || ShowUpload;

    public int VisibleMetricCount =>
        (ShowDownload ? 1 : 0) + (ShowUpload ? 1 : 0) + (ShowPing ? 1 : 0);

    private static string SettingsPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppConstants.AppName,
            "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            settings.Normalize();
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        Normalize();
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, JsonOptions));
    }

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(PingHost))
            PingHost = AppConstants.DefaultPingHost;

        if (!Enum.IsDefined(TextSize))
            TextSize = TextSize.Medium;

        Opacity = Math.Clamp(Opacity, AppConstants.OpacityPresets[0], 1.0);

        if (VisibleMetricCount == 0)
            ShowDownload = true;
    }
}
