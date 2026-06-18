namespace PingIt;

internal readonly record struct MetricSnapshot(double DownloadMbps, double UploadMbps, long LatencyMs);

internal sealed class MonitorSession : IDisposable
{
    private readonly NetworkMonitor _network = new();
    private readonly PingMonitor _ping = new();

    public MetricSnapshot Snapshot => new(_network.DownloadMbps, _network.UploadMbps, _ping.LatencyMs);

    public bool SampleNetwork(bool trackDownload, bool trackUpload)
    {
        if (!trackDownload && !trackUpload)
            return false;

        return _network.Sample();
    }

    public bool ReadLatencyChanged(long previousLatencyMs)
    {
        var current = _ping.LatencyMs;
        return current != previousLatencyMs;
    }

    public void RequestPing(string host) => _ping.RequestPing(host);

    public void ResetPing() => _ping.Reset();

    public void Dispose() => _ping.Dispose();
}
