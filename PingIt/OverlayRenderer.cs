namespace PingIt;

internal sealed class OverlayRenderer : IDisposable
{
    private const int PadX = 12;
    private const int PadY = 10;
    private const int RowGap = 4;

    private Font _dataFont = null!;
    private Pen? _borderPen;
    private SolidBrush? _iconBrush;
    private SolidBrush? _valueBrush;
    private bool _initialized;

    public void ApplyTheme(TextSize textSize)
    {
        _dataFont?.Dispose();
        _dataFont = CreateDataFont(textSize.ToFontSize());

        _borderPen?.Dispose();
        _iconBrush?.Dispose();
        _valueBrush?.Dispose();

        _borderPen = new Pen(AppConstants.BorderColor);
        _iconBrush = new SolidBrush(AppConstants.IconColor);
        _valueBrush = new SolidBrush(AppConstants.ValueColor);
        _initialized = true;
    }

    private static Font CreateDataFont(float size)
    {
        try
        {
            return new Font(AppConstants.FontFamily, size, FontStyle.Regular, GraphicsUnit.Point);
        }
        catch
        {
            return new Font(SystemFonts.MessageBoxFont?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, size, FontStyle.Regular, GraphicsUnit.Point);
        }
    }

    public Size MeasureClientSize(AppSettings settings)
    {
        if (!_initialized)
            ApplyTheme(settings.TextSize);

        var rowHeight = _dataFont!.Height + RowGap;
        var height = PadY * 2 + settings.VisibleMetricCount * rowHeight - RowGap;
        return new Size(settings.TextSize.ToOverlayWidth(), height);
    }

    public void Draw(Graphics g, int width, int height, AppSettings settings, in MetricSnapshot metrics, bool interactive)
    {
        if (!_initialized)
            return;

        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        if (interactive)
        {
            using var movePen = new Pen(Color.FromArgb(180, 120, 170, 255), 1.5f)
            {
                DashStyle = System.Drawing.Drawing2D.DashStyle.Dash
            };
            g.DrawRectangle(movePen, 1, 1, width - 3, height - 3);
        }

        g.DrawRectangle(_borderPen!, 0, 0, width - 1, height - 1);

        var rowHeight = _dataFont!.Height + RowGap;
        var y = PadY;

        if (settings.ShowDownload)
        {
            DrawRow(g, width, "▼", MetricFormatter.Speed(metrics.DownloadMbps), y);
            y += rowHeight;
        }

        if (settings.ShowUpload)
        {
            DrawRow(g, width, "▲", MetricFormatter.Speed(metrics.UploadMbps), y);
            y += rowHeight;
        }

        if (settings.ShowPing)
            DrawRow(g, width, "●", MetricFormatter.Latency(metrics.LatencyMs), y);

        if (interactive)
            DrawCloseButton(g, width);
    }

    public static Rectangle CloseButtonRect(int width) => new(width - 22, 6, 14, 14);

    private void DrawCloseButton(Graphics g, int width)
    {
        var rect = CloseButtonRect(width);
        using var brush = new SolidBrush(Color.FromArgb(170, 200, 200, 210));
        using var font = new Font(AppConstants.FontFamily, 8f, FontStyle.Bold);
        g.DrawString("×", font, brush, rect.X - 1, rect.Y - 3);
    }

    private void DrawRow(Graphics g, int width, string icon, string value, int y)
    {
        g.DrawString(icon, _dataFont, _iconBrush!, PadX, y);

        var valueWidth = g.MeasureString(value, _dataFont).Width;
        g.DrawString(value, _dataFont, _valueBrush!, width - PadX - valueWidth, y);
    }

    public void Dispose()
    {
        _dataFont?.Dispose();
        _borderPen?.Dispose();
        _iconBrush?.Dispose();
        _valueBrush?.Dispose();
    }
}
