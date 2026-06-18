using System.Net.NetworkInformation;

namespace PingIt;

internal sealed class NetworkMonitor
{
    private long _lastBytesReceived;
    private long _lastBytesSent;
    private DateTime _lastSampleUtc = DateTime.MinValue;
    private bool _hasBaseline;

    private IReadOnlyList<NetworkInterface>? _cachedNics;
    private DateTime _nicCacheExpiry = DateTime.MinValue;

    public double DownloadMbps { get; private set; }
    public double UploadMbps { get; private set; }

    public bool Sample()
    {
        var now = DateTime.UtcNow;
        var (received, sent) = GetTotals();

        if (!_hasBaseline)
        {
            _lastBytesReceived = received;
            _lastBytesSent = sent;
            _lastSampleUtc = now;
            _hasBaseline = true;
            DownloadMbps = 0;
            UploadMbps = 0;
            return true;
        }

        var elapsed = (now - _lastSampleUtc).TotalSeconds;
        if (elapsed <= 0)
            return false;

        var nextDownload = Math.Max(0, received - _lastBytesReceived) * 8.0 / elapsed / 1_000_000.0;
        var nextUpload = Math.Max(0, sent - _lastBytesSent) * 8.0 / elapsed / 1_000_000.0;

        var changed = MetricFormatter.ValueChanged(DownloadMbps, nextDownload)
            || MetricFormatter.ValueChanged(UploadMbps, nextUpload);

        DownloadMbps = nextDownload;
        UploadMbps = nextUpload;
        _lastBytesReceived = received;
        _lastBytesSent = sent;
        _lastSampleUtc = now;

        return changed;
    }

    private (long received, long sent) GetTotals()
    {
        long received = 0;
        long sent = 0;

        foreach (var nic in GetActiveInterfaces())
        {
            var stats = nic.GetIPv4Statistics();
            received += stats.BytesReceived;
            sent += stats.BytesSent;
        }

        return (received, sent);
    }

    private IReadOnlyList<NetworkInterface> GetActiveInterfaces()
    {
        var now = DateTime.UtcNow;
        if (_cachedNics is not null && now < _nicCacheExpiry)
            return _cachedNics;

        _cachedNics = NetworkInterface.GetAllNetworkInterfaces()
            .Where(IsIncludedInterface)
            .ToArray();
        _nicCacheExpiry = now.AddSeconds(AppConstants.NicCacheRefreshSeconds);

        return _cachedNics;
    }

    private static bool IsIncludedInterface(NetworkInterface nic)
    {
        if (nic.OperationalStatus != OperationalStatus.Up)
            return false;

        if (nic.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
            return false;

        var name = nic.Description;
        return !(name.Contains("Virtual", StringComparison.OrdinalIgnoreCase) &&
                 !name.Contains("VPN", StringComparison.OrdinalIgnoreCase) &&
                 !name.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase));
    }
}
