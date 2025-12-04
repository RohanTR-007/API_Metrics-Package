namespace ApiStats.Dashboard.Options;

public class ApiStatsOptions
{
    public HashSet<string> ExcludedPaths { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "/favicon.ico",
        "/apiMetricDashboard",
        "/apiMetricDashboard-data"
    };

    public HashSet<string> ExcludedPathPrefixes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> MaskQueryParameters { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "token", "access_token", "api_key"
    };

    public HashSet<string> MaskHeaders { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "authorization", "cookie", "set-cookie"
    };

    public int MaxErrorRecordsPerEndpoint { get; set; } = 50;

    // for P50/P95/P99 (rolling window)
    public int MaxSamplesPerEndpointForPercentiles { get; set; } = 200;

    // threshold for treating a request as 'slow' (optional, for future)
    public long SlowRequestThresholdMs { get; set; } = 2000;

    // enable/disable publishing Prometheus metrics
    public bool EnablePrometheus { get; set; } = true;
}
