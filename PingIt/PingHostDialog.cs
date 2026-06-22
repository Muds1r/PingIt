namespace PingIt;

internal sealed class PingHostDialog : Form
{
    private readonly TextBox _hostInput = new() { Width = 280 };

    public PingHostDialog(string currentHost)
    {
        Text = "Ping host";
        Icon = AppIcons.Tray;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(360, 140);
        BackColor = AppConstants.BackgroundColor;
        ForeColor = AppConstants.ValueColor;

        var label = new Label
        {
            Text = "Hostname or IP address:",
            AutoSize = true,
            Location = new Point(20, 20)
        };

        _hostInput.Location = new Point(20, 48);
        _hostInput.Text = currentHost;

        var okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(252, 92),
            Size = new Size(88, 28)
        };
        okButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_hostInput.Text))
            {
                MessageBox.Show(this, "Enter a hostname or IP address.", AppConstants.AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.None;
            }
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(156, 92),
            Size = new Size(88, 28)
        };

        AcceptButton = okButton;
        CancelButton = cancelButton;
        Controls.AddRange([label, _hostInput, okButton, cancelButton]);
    }

    public string Host => _hostInput.Text.Trim();
}
