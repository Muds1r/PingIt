using System.Text;

namespace PingIt;

internal static class CrashReporter
{
    private static int _shown;

    private static string LogDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppConstants.AppName,
            "logs");

    public static string LatestLogPath => Path.Combine(LogDirectory, "latest-error.log");

    public static void Install()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => Handle(e.Exception, "UI thread");
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                Handle(ex, "Unhandled", e.IsTerminating);
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Handle(e.Exception, "Background task");
            e.SetObserved();
        };
    }

    public static void Handle(Exception ex, string context, bool terminating = false)
    {
        var report = BuildReport(ex, context, terminating);
        var logPath = WriteLog(report);

        if (Interlocked.Exchange(ref _shown, 1) != 0)
            return;

        try
        {
            using var dialog = new ErrorReportForm(ex, context, report, logPath, terminating);
            dialog.ShowDialog();
        }
        catch
        {
            MessageBox.Show(
                $"PingIt encountered an error.\n\n{ex.Message}\n\nFull details:\n{logPath}",
                AppConstants.AppName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static string BuildReport(Exception ex, string context, bool terminating)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{AppConstants.AppName} error report");
        sb.AppendLine($"Time      : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"OS        : {Environment.OSVersion}");
        sb.AppendLine($"Runtime   : {Environment.Version}");
        sb.AppendLine($"Context   : {context}");
        sb.AppendLine($"Terminating: {terminating}");
        sb.AppendLine($"Executable: {Application.ExecutablePath}");
        sb.AppendLine();
        AppendException(sb, ex);
        return sb.ToString();
    }

    private static void AppendException(StringBuilder sb, Exception ex, int depth = 0)
    {
        var indent = new string(' ', depth * 2);
        sb.AppendLine($"{indent}[{ex.GetType().Name}] {ex.Message}");
        if (!string.IsNullOrWhiteSpace(ex.StackTrace))
        {
            foreach (var line in ex.StackTrace.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                sb.AppendLine($"{indent}  {line.Trim()}");
        }

        if (ex.InnerException is not null)
        {
            sb.AppendLine($"{indent}Inner exception:");
            AppendException(sb, ex.InnerException, depth + 1);
        }
    }

    private static string WriteLog(string report)
    {
        Directory.CreateDirectory(LogDirectory);

        var timestampedPath = Path.Combine(LogDirectory, $"error-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        File.WriteAllText(timestampedPath, report);
        File.WriteAllText(LatestLogPath, report);
        return timestampedPath;
    }
}
