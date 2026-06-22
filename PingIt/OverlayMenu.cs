namespace PingIt;

internal sealed class OverlayMenu
{
    private readonly AppSettings _settings;
    private readonly Action _onChanged;
    private readonly ToolStripMenuItem _showDownloadItem;
    private readonly ToolStripMenuItem _showUploadItem;
    private readonly ToolStripMenuItem _showPingItem;
    private readonly ToolStripMenuItem _startWithWindowsItem;
    private readonly ToolStripMenuItem[] _opacityItems;
    private readonly ToolStripMenuItem[] _textSizeItems;
    private readonly ToolStripMenuItem[] _pingHostItems;
    private bool _syncing;

    public OverlayMenu(AppSettings settings, Action onChanged)
    {
        _settings = settings;
        _onChanged = onChanged;

        _showDownloadItem = CreateToggle("Download", OnVisibilityChanged);
        _showUploadItem = CreateToggle("Upload", OnVisibilityChanged);
        _showPingItem = CreateToggle("Ping", OnVisibilityChanged);
        _startWithWindowsItem = CreateToggle("Start with Windows", OnStartupChanged);
        _opacityItems = CreateOpacityItems();
        _textSizeItems = CreateTextSizeItems();
        _pingHostItems = CreatePingHostItems();
    }

    public void AppendTo(ToolStripItemCollection items)
    {
        var showMenu = new ToolStripMenuItem("Show");
        showMenu.DropDownItems.AddRange([_showDownloadItem, _showUploadItem, _showPingItem]);

        var transparencyMenu = new ToolStripMenuItem("Transparency");
        transparencyMenu.DropDownItems.AddRange(_opacityItems);

        var textSizeMenu = new ToolStripMenuItem("Text size");
        textSizeMenu.DropDownItems.AddRange(_textSizeItems);

        var pingHostMenu = new ToolStripMenuItem("Ping host");
        pingHostMenu.DropDownItems.AddRange(_pingHostItems);
        pingHostMenu.DropDownItems.Add(new ToolStripSeparator());
        pingHostMenu.DropDownItems.Add("Custom...", null, (_, _) => ChooseCustomPingHost());

        items.Add(showMenu);
        items.Add(transparencyMenu);
        items.Add(textSizeMenu);
        items.Add(pingHostMenu);
        items.Add(new ToolStripSeparator());
        items.Add(_startWithWindowsItem);

        Sync();
    }

    public void Sync()
    {
        _syncing = true;

        _showDownloadItem.Checked = _settings.ShowDownload;
        _showUploadItem.Checked = _settings.ShowUpload;
        _showPingItem.Checked = _settings.ShowPing;
        _startWithWindowsItem.Checked = _settings.StartWithWindows;

        MarkClosest(_opacityItems, _settings.Opacity, item => (double)item.Tag!);
        foreach (var item in _textSizeItems)
            item.Checked = (TextSize)item.Tag! == _settings.TextSize;

        foreach (var item in _pingHostItems)
            item.Checked = string.Equals((string)item.Tag!, _settings.PingHost, StringComparison.OrdinalIgnoreCase);

        _syncing = false;
    }

    private ToolStripMenuItem[] CreateOpacityItems() =>
        AppConstants.OpacityPresets
            .Select(opacity =>
            {
                var item = new ToolStripMenuItem($"{opacity * 100:0}%") { Tag = opacity };
                item.Click += (_, _) =>
                {
                    _settings.Opacity = opacity;
                    _onChanged();
                };
                return item;
            })
            .ToArray();

    private ToolStripMenuItem[] CreateTextSizeItems() =>
        AppConstants.TextSizePresets
            .Select(preset =>
            {
                var item = new ToolStripMenuItem(preset.Label) { Tag = preset.Size };
                item.Click += (_, _) =>
                {
                    _settings.TextSize = preset.Size;
                    _onChanged();
                };
                return item;
            })
            .ToArray();

    private ToolStripMenuItem[] CreatePingHostItems() =>
        AppConstants.PingHostPresets
            .Select(host =>
            {
                var item = new ToolStripMenuItem(host) { Tag = host };
                item.Click += (_, _) => SetPingHost(host);
                return item;
            })
            .ToArray();

    private ToolStripMenuItem CreateToggle(string text, Action onChanged)
    {
        var item = new ToolStripMenuItem(text) { CheckOnClick = true };
        item.CheckedChanged += (_, _) =>
        {
            if (!_syncing)
                onChanged();
        };
        return item;
    }

    private void OnVisibilityChanged()
    {
        var download = _showDownloadItem.Checked;
        var upload = _showUploadItem.Checked;
        var ping = _showPingItem.Checked;

        if (!download && !upload && !ping)
        {
            Sync();
            return;
        }

        _settings.ShowDownload = download;
        _settings.ShowUpload = upload;
        _settings.ShowPing = ping;
        _onChanged();
    }

    private void OnStartupChanged()
    {
        _settings.StartWithWindows = _startWithWindowsItem.Checked;
        if (!StartupHelper.SetEnabled(_settings.StartWithWindows))
        {
            _settings.StartWithWindows = false;
            Sync();
            MessageBox.Show(
                "Windows would not save the startup setting. Try running PingIt once as your normal user account.",
                AppConstants.AppName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _settings.Save();
    }

    private void SetPingHost(string host)
    {
        _settings.PingHost = host;
        _onChanged();
    }

    private void ChooseCustomPingHost()
    {
        using var dialog = new PingHostDialog(_settings.PingHost);
        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        SetPingHost(dialog.Host);
    }

    private static void MarkClosest(ToolStripMenuItem[] items, double value, Func<ToolStripMenuItem, double> read)
    {
        var closest = items.MinBy(item => Math.Abs(read(item) - value));
        foreach (var item in items)
            item.Checked = item == closest;
    }
}
