namespace PingIt;

internal static class Program
{
    private const string SingleInstanceMutexName = @"Global\PingIt_SingleInstance";

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        CrashReporter.Install();

        try
        {
            RunApp();
        }
        catch (Exception ex)
        {
            CrashReporter.Handle(ex, "Startup");
        }
    }

    private static void RunApp()
    {
        using var mutex = new Mutex(true, SingleInstanceMutexName, out var isNewInstance);
        if (!isNewInstance)
            return;

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

        Application.Run(new OverlayForm(settings, isFirstRun));
    }
}
