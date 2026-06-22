using System.Net.NetworkInformation;

namespace PingIt;

internal sealed class PingMonitor : IDisposable
{
    private readonly Ping _ping = new();
    private readonly object _gate = new();
    private CancellationTokenSource _cts = new();
    private int _busy;
    private long _latencyMs = -1;
    private bool _disposed;

    public long LatencyMs => Volatile.Read(ref _latencyMs);

    public void RequestPing(string host)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
            return;

        _ = RunPingAsync(host);
    }

    private async Task RunPingAsync(string host)
    {
        CancellationToken token;
        lock (_gate)
            token = _cts.Token;

        try
        {
            var reply = await _ping.SendPingAsync(host, AppConstants.PingTimeoutMs, buffer: null, options: null, token);
            Volatile.Write(ref _latencyMs, reply.Status == IPStatus.Success ? reply.RoundtripTime : -1);
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
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

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        lock (_gate)
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        _ping.Dispose();
    }
}
