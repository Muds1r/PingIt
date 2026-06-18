using System.Net.NetworkInformation;

namespace PingIt;

internal sealed class PingMonitor : IDisposable
{
    private readonly Ping _ping = new();
    private int _busy;
    private long _latencyMs = -1;

    public long LatencyMs => Volatile.Read(ref _latencyMs);

    public void RequestPing(string host)
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
            return;

        _ = RunPingAsync(host);
    }

    private async Task RunPingAsync(string host)
    {
        try
        {
            var reply = await _ping.SendPingAsync(host, AppConstants.PingTimeoutMs);
            Volatile.Write(ref _latencyMs, reply.Status == IPStatus.Success ? reply.RoundtripTime : -1);
        }
        catch
        {
            Volatile.Write(ref _latencyMs, -1);
        }
        finally
        {
            Volatile.Write(ref _busy, 0);
        }
    }

    public void Reset() => Volatile.Write(ref _latencyMs, -1);

    public void Dispose() => _ping.Dispose();
}
