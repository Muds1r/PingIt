namespace PingIt;

internal static class MetricFormatter
{
    public static string Speed(double mbps) =>
        mbps >= 100 ? $"{mbps:F0} Mbps" :
        mbps >= 10 ? $"{mbps:F1} Mbps" :
        $"{mbps:F2} Mbps";

    public static string Latency(long ms) =>
        ms < 0 ? "— ms" : $"{ms} ms";

    public static bool ValueChanged(double previous, double next, double epsilon = 0.004) =>
        Math.Abs(previous - next) > epsilon;
}
