namespace PingIt;

internal sealed class TrayHost : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _moveOverlayItem;
    private readonly ToolStripMenuItem _showOverlayItem;
    private bool _menuOpen;

    public TrayHost(
        AppSettings settings,
        OverlayMenu menu,
        Action<bool> setInteractiveMode,
        Action<bool> setOverlayVisible,
        Action exit)
    {
        SetInteractiveMode = setInteractiveMode;
        SetOverlayVisible = setOverlayVisible;

        _moveOverlayItem = new ToolStripMenuItem("Move overlay")
        {
            CheckOnClick = true,
            ToolTipText = "Unlock the overlay so you can drag it"
        };
        _moveOverlayItem.CheckedChanged += (_, _) => UpdateInteractiveMode();

        _showOverlayItem = new ToolStripMenuItem("Show overlay", null, (_, _) => ToggleOverlayVisibility())
        {
            Checked = true,
            CheckOnClick = true
        };

        var trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add(_showOverlayItem);
        trayMenu.Items.Add(_moveOverlayItem);
        trayMenu.Items.Add(new ToolStripSeparator());
        foreach (ToolStripItem item in menu.Menu.Items)
            trayMenu.Items.Add(item);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Exit", null, (_, _) => exit());

        trayMenu.Opening += (_, _) =>
        {
            _menuOpen = true;
            UpdateInteractiveMode();
        };
        trayMenu.Closed += (_, _) =>
        {
            _menuOpen = false;
            UpdateInteractiveMode();
        };

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = AppConstants.AppName,
            Visible = true,
            ContextMenuStrip = trayMenu
        };
        _notifyIcon.DoubleClick += (_, _) => ToggleOverlayVisibility();
    }

    private Action<bool> SetInteractiveMode { get; }
    private Action<bool> SetOverlayVisible { get; }

    public void BeginPositioningMode() => _moveOverlayItem.Checked = true;

    public void SyncOverlayVisibility(bool visible)
    {
        _showOverlayItem.Checked = visible;
    }

    private void ToggleOverlayVisibility()
    {
        _showOverlayItem.Checked = !_showOverlayItem.Checked;
        SetOverlayVisible(_showOverlayItem.Checked);
    }

    private void UpdateInteractiveMode() =>
        SetInteractiveMode(_menuOpen || _moveOverlayItem.Checked);

    public void Dispose() => _notifyIcon.Dispose();
}
