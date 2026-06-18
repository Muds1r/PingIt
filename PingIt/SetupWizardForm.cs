namespace PingIt;

internal sealed class SetupWizardForm : Form
{
    private readonly CheckBox _download = new() { Text = "Download speed", Checked = true, AutoSize = true };
    private readonly CheckBox _upload = new() { Text = "Upload speed", Checked = true, AutoSize = true };
    private readonly CheckBox _ping = new() { Text = "Ping (ms)", Checked = true, AutoSize = true };

    public SetupWizardForm()
    {
        Text = "Welcome to PingIt";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(360, 250);
        BackColor = AppConstants.BackgroundColor;
        ForeColor = AppConstants.ValueColor;

        var title = new Label
        {
            Text = "Choose what to display",
            AutoSize = true,
            Font = new Font(AppConstants.FontFamily, 12f, FontStyle.Bold),
            Location = new Point(24, 20)
        };

        var subtitle = new Label
        {
            Text = "Pick at least one stat. You can change this anytime from the tray icon near the clock.",
            Location = new Point(24, 52),
            Size = new Size(312, 40)
        };

        _download.Location = new Point(28, 104);
        _upload.Location = new Point(28, 132);
        _ping.Location = new Point(28, 160);

        var continueButton = new Button
        {
            Text = "Continue",
            DialogResult = DialogResult.OK,
            Location = new Point(252, 206),
            Size = new Size(88, 30)
        };
        continueButton.Click += (_, _) =>
        {
            if (!_download.Checked && !_upload.Checked && !_ping.Checked)
            {
                MessageBox.Show(this, "Select at least one stat to display.", AppConstants.AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.None;
            }
        };

        AcceptButton = continueButton;
        Controls.AddRange([title, subtitle, _download, _upload, _ping, continueButton]);
    }

    public void ApplyTo(AppSettings settings)
    {
        settings.ShowDownload = _download.Checked;
        settings.ShowUpload = _upload.Checked;
        settings.ShowPing = _ping.Checked;
        settings.SetupCompleted = true;
    }
}
