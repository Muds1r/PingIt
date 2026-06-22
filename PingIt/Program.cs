namespace PingIt;

internal static class Program
{
    private static readonly string SingleInstanceMutexName = $"Local\\{AppConstants.AppName}_SingleInstance";

    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        CrashReporter.Install();

        try
        {
            RunApp(args);
        }
        catch (Exception ex)
        {
            CrashReporter.Handle(ex, "Startup");
        }
    }

    private static void RunApp(string[] args)
    {
        using var mutex = new Mutex(true, SingleInstanceMutexName, out var isNewInstance);
        if (!isNewInstance)
        {
            MessageBox.Show(
                "PingIt is already running.\n\nCheck the system tray near the clock.",
                AppConstants.AppName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var launchedAtStartup = args.Any(static a =>
            string.Equals(a, "--startup", StringComparison.OrdinalIgnoreCase));

        var settings = AppSettings.Load();
        var isFirstRun = !settings.SetupCompleted;

        if (isFirstRun)
        {
            using var wizard = new SetupWizardForm();
            if (wizard.ShowDialog() != DialogResult.OK)
                return;

            wizard.ApplyTo(settings);
            settings.Save();
        }

        Application.Run(new OverlayForm(settings, isFirstRun, launchedAtStartup));
    }
}
