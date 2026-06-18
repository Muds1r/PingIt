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

    public void ApplyTheme(TextSize textSize)
    {
        _dataFont.Dispose();
        _dataFont = new Font(AppConstants.FontFamily, textSize.ToFontSize(), FontStyle.Regular, GraphicsUnit.Point);

        _borderPen?.Dispose();
        _iconBrush?.Dispose();
        _valueBrush?.Dispose();

        _borderPen = new Pen(AppConstants.BorderColor);
        _iconBrush = new SolidBrush(AppConstants.IconColor);
        _valueBrush = new SolidBrush(AppConstants.ValueColor);
    }

    public Size MeasureClientSize(AppSettings settings)
    {
        var rowHeight = _dataFont.Height + RowGap;
        var height = PadY * 2 + settings.VisibleMetricCount * rowHeight - RowGap;
        return new Size(settings.TextSize.ToOverlayWidth(), height);
    }

    public void Draw(Graphics g, int width, int height, AppSettings settings, in MetricSnapshot metrics, bool interactive)
    {
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

        var rowHeight = _dataFont.Height + RowGap;
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
    }

    private void DrawRow(Graphics g, int width, string icon, string value, int y)
    {
        g.DrawString(icon, _dataFont, _iconBrush!, PadX, y);

        var valueWidth = g.MeasureString(value, _dataFont).Width;
        g.DrawString(value, _dataFont, _valueBrush!, width - PadX - valueWidth, y);
    }

    public void Dispose()
    {
        _dataFont.Dispose();
        _borderPen?.Dispose();
        _iconBrush?.Dispose();
        _valueBrush?.Dispose();
    }
}
