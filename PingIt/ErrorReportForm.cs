namespace PingIt;

internal sealed class ErrorReportForm : Form
{
    public ErrorReportForm(Exception ex, string context, string report, string logPath, bool terminating)
    {
        Text = $"{AppConstants.AppName} — Error";
        Icon = SystemIcons.Error;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(560, 440);
        BackColor = Color.FromArgb(24, 24, 28);
        ForeColor = Color.FromArgb(235, 237, 240);

        var title = new Label
        {
            Text = terminating ? "PingIt stopped because of an error." : "PingIt hit an error.",
            AutoSize = false,
            Size = new Size(520, 40),
            Location = new Point(20, 16),
            Font = new Font(SystemFonts.MessageBoxFont?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, 11f, FontStyle.Bold)
        };

        var summary = new Label
        {
            Text = $"{context}: {ex.Message}",
            AutoSize = false,
            Size = new Size(520, 48),
            Location = new Point(20, 56)
        };

        var detailsLabel = new Label
        {
            Text = "Error log:",
            AutoSize = true,
            Location = new Point(20, 108)
        };

        var details = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Location = new Point(20, 132),
            Size = new Size(520, 220),
            Text = report,
            BackColor = Color.FromArgb(18, 18, 22),
            ForeColor = Color.FromArgb(235, 237, 240),
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 9f)
        };

        var logPathLabel = new Label
        {
            Text = $"Saved to: {logPath}",
            AutoSize = false,
            Size = new Size(520, 20),
            Location = new Point(20, 362)
        };

        var openFolderButton = new Button
        {
            Text = "Open log folder",
            Location = new Point(20, 392),
            Size = new Size(120, 28)
        };
        openFolderButton.Click += (_, _) =>
        {
            try
            {
                var folder = Path.GetDirectoryName(logPath)!;
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });
            }
            catch (Exception openEx)
            {
                MessageBox.Show(this, openEx.Message, AppConstants.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        var closeButton = new Button
        {
            Text = "Close",
            DialogResult = DialogResult.OK,
            Location = new Point(452, 392),
            Size = new Size(88, 28)
        };

        AcceptButton = closeButton;
        Controls.AddRange([title, summary, detailsLabel, details, logPathLabel, openFolderButton, closeButton]);
    }
}
