namespace PingIt;

internal sealed class OverlayForm : Form
{
    private readonly AppSettings _settings;
    private readonly MonitorSession _session = new();
    private readonly OverlayRenderer _renderer = new();
    private readonly OverlayMenu _menu;
    private readonly TrayHost _tray;
    private readonly System.Windows.Forms.Timer _sampleTimer;
    private readonly System.Windows.Forms.Timer _pingTimer;
    private readonly System.Windows.Forms.Timer _topMostTimer;

    private bool _allowClose;
    private bool _interactiveMode;
    private bool _dragging;
    private Point _dragOffset;
    private long _lastLatencyMs = -1;

    public OverlayForm(AppSettings settings, bool isFirstRun)
    {
        _settings = settings;
        StartupHelper.SetEnabled(_settings.StartWithWindows);

        ConfigureWindow();
        _menu = new OverlayMenu(_settings, OnSettingsChanged);
        _tray = new TrayHost(
            _menu,
            SetInteractiveMode,
            SetOverlayVisible,
            RequestExit);

        _sampleTimer = CreateTimer(AppConstants.NetworkSampleIntervalMs, OnSampleTick);
        _pingTimer = CreateTimer(AppConstants.PingIntervalMs, OnPingTick);
        _topMostTimer = CreateTimer(AppConstants.TopMostRefreshIntervalMs, (_, _) => Win32Window.EnsureTopMost(Handle));

        ApplyAppearance();

        if (!_settings.OverlayVisible)
            HideToTray(showHint: false);
        else
            ApplyTaskbarVisibility(visible: true);

        if (isFirstRun)
        {
            _tray.BeginPositioningMode();
            MessageBox.Show(
                "Drag the overlay to your preferred spot.\n\n" +
                "When finished, open the PingIt icon near the clock and turn off \"Move overlay\".\n\n" +
                "Closing the overlay hides it to the tray — PingIt keeps running in the background, like Discord.",
                AppConstants.AppName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_TOPMOST = 0x00000008;
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOPMOST;
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Win32Window.EnsureTopMost(Handle);
        SetInteractiveMode(_interactiveMode);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (_settings.OverlayVisible)
            ResumeMonitoring();
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        if (Visible)
            Win32Window.EnsureTopMost(Handle);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            HideToTray(showHint: true);
            return;
        }

        PersistPosition();
        _settings.Save();

        _sampleTimer.Stop();
        _pingTimer.Stop();
        _topMostTimer.Stop();
        _session.Dispose();
        _renderer.Dispose();
        _tray.Dispose();

        base.OnFormClosing(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        _renderer.Draw(e.Graphics, ClientSize.Width, ClientSize.Height, _settings, _session.Snapshot, _interactiveMode);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (_interactiveMode && e.Button == MouseButtons.Left &&
            OverlayRenderer.CloseButtonRect(ClientSize.Width).Contains(e.Location))
        {
            HideToTray(showHint: true);
            return;
        }

        if (!_interactiveMode || e.Button != MouseButtons.Left)
            return;

        _dragging = true;
        _dragOffset = e.Location;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_dragging)
            return;

        Location = new Point(Location.X + e.X - _dragOffset.X, Location.Y + e.Y - _dragOffset.Y);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButtons.Left)
            _dragging = false;
    }

    private void ConfigureWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(_settings.X, _settings.Y);
        TopMost = true;
        Text = AppConstants.AppName;
        BackColor = AppConstants.BackgroundColor;
        DoubleBuffered = true;
    }

    private static System.Windows.Forms.Timer CreateTimer(int interval, EventHandler onTick)
    {
        var timer = new System.Windows.Forms.Timer { Interval = interval };
        timer.Tick += onTick;
        return timer;
    }

    private void OnSampleTick(object? sender, EventArgs e)
    {
        var networkChanged = _session.SampleNetwork(_settings.ShowDownload, _settings.ShowUpload);
        var latencyChanged = _settings.ShowPing && _session.ReadLatencyChanged(_lastLatencyMs);

        if (!networkChanged && !latencyChanged)
            return;

        _lastLatencyMs = _session.Snapshot.LatencyMs;
        Invalidate();
    }

    private void OnPingTick(object? sender, EventArgs e) =>
        _session.RequestPing(_settings.PingHost);

    private void OnSettingsChanged()
    {
        Opacity = _settings.Opacity;
        ConfigurePingTimer(start: _settings.ShowPing);
        ApplyAppearance();
        _menu.Sync();
        _settings.Save();
    }

    private void ApplyAppearance()
    {
        Opacity = _settings.Opacity;
        _renderer.ApplyTheme(_settings.TextSize);
        ClientSize = _renderer.MeasureClientSize(_settings);
        Invalidate();
    }

    private void ConfigurePingTimer(bool start)
    {
        if (start)
        {
            if (!_pingTimer.Enabled)
            {
                _pingTimer.Start();
                _session.RequestPing(_settings.PingHost);
            }
        }
        else
        {
            _pingTimer.Stop();
            _session.ResetPing();
            _lastLatencyMs = -1;
        }
    }

    private void SetInteractiveMode(bool enabled)
    {
        _interactiveMode = enabled;
        if (IsHandleCreated)
            Win32Window.SetClickThrough(Handle, !enabled);

        Cursor = enabled ? Cursors.SizeAll : Cursors.Default;
        Invalidate();
    }

    private void SetOverlayVisible(bool visible)
    {
        if (visible)
        {
            _settings.OverlayVisible = true;
            Show();
            ApplyTaskbarVisibility(visible: true);
            Win32Window.EnsureTopMost(Handle);
            ResumeMonitoring();
        }
        else
        {
            HideToTray(showHint: false);
        }

        _tray.SyncOverlayVisibility(visible);
        _settings.Save();
    }

    private void HideToTray(bool showHint)
    {
        PersistPosition();
        _settings.OverlayVisible = false;
        Hide();
        ApplyTaskbarVisibility(visible: false);
        _tray.SyncOverlayVisibility(false);
        PauseMonitoring();

        if (showHint && !_settings.TrayCloseHintShown)
        {
            _settings.TrayCloseHintShown = true;
            _tray.NotifyStillRunning();
        }

        _settings.Save();
    }

    private void ResumeMonitoring()
    {
        _sampleTimer.Start();
        ConfigurePingTimer(start: _settings.ShowPing);
        _topMostTimer.Start();
    }

    private void PauseMonitoring()
    {
        _topMostTimer.Stop();
        _sampleTimer.Stop();
        _pingTimer.Stop();
    }

    private void ApplyTaskbarVisibility(bool visible) => ShowInTaskbar = visible;

    private void PersistPosition()
    {
        _settings.X = Location.X;
        _settings.Y = Location.Y;
    }

    private void RequestExit()
    {
        _allowClose = true;
        Close();
    }
}
