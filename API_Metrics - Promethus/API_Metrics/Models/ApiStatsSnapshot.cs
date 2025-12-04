using System.Collections.Generic;

namespace ApiStats.Dashboard.Models;

public class ApiStatsSnapshot
{
    public long TotalRequests { get; set; }
    public long TotalSuccess { get; set; }
    public long TotalFailed { get; set; }
    public double AverageDurationMs { get; set; }

    public List<EndpointStatsSnapshot> Endpoints { get; set; } = new();
}

public class EndpointStatsSnapshot
{
    public string Endpoint { get; set; } = "";
    public string Method { get; set; } = "";
    public long TotalRequests { get; set; }
    public long SuccessRequests { get; set; }
    public long FailedRequests { get; set; }
    public double AverageDurationMs { get; set; }
    public double P50DurationMs { get; set; }
    public double P95DurationMs { get; set; }
    public double P99DurationMs { get; set; }
    public Dictionary<int, long> StatusCodeCounts { get; set; } = new();
    public List<ErrorRecord> RecentErrors { get; set; } = new();
}
