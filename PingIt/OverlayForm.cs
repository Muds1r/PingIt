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
    private const int StartupActivationDelayMs = 2000;

    private readonly System.Windows.Forms.Timer? _startupTimer;
    private long _lastLatencyMs = -1;

    public OverlayForm(AppSettings settings, bool isFirstRun, bool launchedAtStartup = false)
    {
        _settings = settings;
        StartupHelper.SetEnabled(_settings.StartWithWindows);

        ConfigureWindow();
        _menu = new OverlayMenu(_settings, OnSettingsChanged);
        _tray = new TrayHost(
            _menu,
            _settings.OverlayVisible,
            SetInteractiveMode,
            SetOverlayVisible,
            RequestExit);

        _sampleTimer = CreateTimer(AppConstants.NetworkSampleIntervalMs, OnSampleTick);
        _pingTimer = CreateTimer(AppConstants.PingIntervalMs, OnPingTick);
        _topMostTimer = CreateTimer(AppConstants.TopMostRefreshIntervalMs, OnTopMostTick);

        ApplyAppearance();
        ApplyClampedLocation();

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
        else if (launchedAtStartup)
        {
            _startupTimer = CreateTimer(StartupActivationDelayMs, OnStartupActivation);
            _startupTimer.Start();
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
        EnsureMonitoringRunning();
        UpdateTopMostTimer();
        Invalidate();
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
        _startupTimer?.Stop();
        _session.Dispose();
        _renderer.Dispose();
        _tray.Dispose();
        AppIcons.DisposeCached();

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
        if (e.Button != MouseButtons.Left)
            return;

        if (_dragging)
            _tray.EndPositioningMode();

        _dragging = false;
    }

    private void OnStartupActivation(object? sender, EventArgs e)
    {
        _startupTimer?.Stop();

        try
        {
            _session.InvalidateNetworkCache();
            EnsureMonitoringRunning();

            if (_settings.OverlayVisible)
            {
                Show();
                ApplyTaskbarVisibility(visible: true);
                Win32Window.EnsureTopMost(Handle);
                SetInteractiveMode(false);
            }

            _lastLatencyMs = _session.Snapshot.LatencyMs;
            Invalidate();
        }
        catch (Exception ex)
        {
            CrashReporter.Handle(ex, "Startup activation", showDialog: false);
        }
    }

    private void OnTopMostTick(object? sender, EventArgs e)
    {
        if (!IsHandleCreated || IsDisposed)
            return;

        Win32Window.EnsureTopMost(Handle);
    }

    private void ConfigureWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(_settings.X, _settings.Y);
        TopMost = true;
        Text = AppConstants.AppName;
        Icon = AppIcons.Tray;
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
        try
        {
            var networkChanged = _session.SampleNetwork(_settings.ShowDownload, _settings.ShowUpload);
            var latencyChanged = _settings.ShowPing && _session.ReadLatencyChanged(_lastLatencyMs);

            if (!networkChanged && !latencyChanged)
                return;

            _lastLatencyMs = _session.Snapshot.LatencyMs;
            if (Visible && _settings.OverlayVisible)
                Invalidate();
        }
        catch (Exception ex)
        {
            CrashReporter.Handle(ex, "Network monitoring", showDialog: false);
        }
    }

    private void OnPingTick(object? sender, EventArgs e)
    {
        try
        {
            _session.RequestPing(_settings.PingHost);
        }
        catch (Exception ex)
        {
            CrashReporter.Handle(ex, "Ping monitoring", showDialog: false);
        }
    }

    private void OnSettingsChanged()
    {
        Opacity = _settings.Opacity;
        ConfigurePingTimer(start: _settings.ShowPing);
        ApplyAppearance();
        ApplyClampedLocation();
        _menu.Sync();
        _settings.Save();

        if (_settings.ShowPing)
            _session.RequestPing(_settings.PingHost);
    }

    private void ApplyAppearance()
    {
        Opacity = _settings.Opacity;
        _renderer.ApplyTheme(_settings.TextSize);
        ClientSize = _renderer.MeasureClientSize(_settings);
        if (Visible && _settings.OverlayVisible)
            Invalidate();
    }

    private void ApplyClampedLocation()
    {
        Location = ScreenPlacement.ClampToVisibleArea(new Point(_settings.X, _settings.Y), ClientSize);
        _settings.X = Location.X;
        _settings.Y = Location.Y;
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
            EnsureMonitoringRunning();
            UpdateTopMostTimer();
            SetInteractiveMode(false);
            Invalidate();
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
        UpdateTopMostTimer();

        if (showHint && !_settings.TrayCloseHintShown)
        {
            _settings.TrayCloseHintShown = true;
            _tray.NotifyStillRunning();
        }

        _settings.Save();
    }

    private void EnsureMonitoringRunning()
    {
        if (!_sampleTimer.Enabled)
            _sampleTimer.Start();

        ConfigurePingTimer(start: _settings.ShowPing);
    }

    private void UpdateTopMostTimer()
    {
        if (Visible && _settings.OverlayVisible)
            _topMostTimer.Start();
        else
            _topMostTimer.Stop();
    }

    private void ApplyTaskbarVisibility(bool visible) => ShowInTaskbar = visible;

    private void PersistPosition()
    {
        var clamped = ScreenPlacement.ClampToVisibleArea(Location, ClientSize);
        Location = clamped;
        _settings.X = clamped.X;
        _settings.Y = clamped.Y;
    }

    private void RequestExit()
    {
        _allowClose = true;
        Close();
    }
}
