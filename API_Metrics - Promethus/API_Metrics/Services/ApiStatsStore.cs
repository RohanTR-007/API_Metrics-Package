using System.Collections.Concurrent;
using ApiStats.Dashboard.Models;
using ApiStats.Dashboard.Options;

namespace ApiStats.Dashboard.Services;

public class ApiStatsStore
{
    private readonly ConcurrentDictionary<(string endpoint, string method), EndpointStats> _endpoints = new();
    private readonly ApiStatsOptions _options;

    public ApiStatsStore(ApiStatsOptions options)
    {
        _options = options;
    }

    public void RecordRequest(string endpoint, string method, int statusCode, long durationMs)
    {
        var key = (endpoint, method);
        var stats = _endpoints.GetOrAdd(
            key,
            k => new EndpointStats(k.endpoint, k.method, _options.MaxSamplesPerEndpointForPercentiles, _options.MaxErrorRecordsPerEndpoint));

        stats.Record(statusCode, durationMs);
    }

    public void RecordError(ErrorRecord err)
    {
        var key = (err.Endpoint, err.Method);
        var stats = _endpoints.GetOrAdd(
            key,
            k => new EndpointStats(k.endpoint, k.method, _options.MaxSamplesPerEndpointForPercentiles, _options.MaxErrorRecordsPerEndpoint));

        stats.RecordError(err);
    }

    public ApiStatsSnapshot GetSnapshot()
    {
        var snapshot = new ApiStatsSnapshot();
        foreach (var kvp in _endpoints)
        {
            var e = kvp.Value;
            snapshot.TotalRequests += e.TotalRequests;
            snapshot.TotalSuccess += e.SuccessRequests;
            snapshot.TotalFailed += e.FailedRequests;

            var (p50, p95, p99) = e.ComputePercentiles();

            snapshot.Endpoints.Add(new EndpointStatsSnapshot
            {
                Endpoint = e.Endpoint,
                Method = e.Method,
                TotalRequests = e.TotalRequests,
                SuccessRequests = e.SuccessRequests,
                FailedRequests = e.FailedRequests,
                AverageDurationMs = e.AverageDurationMs,
                P50DurationMs = p50,
                P95DurationMs = p95,
                P99DurationMs = p99,
                StatusCodeCounts = e.StatusCodeCounts.ToDictionary(x => x.Key, x => x.Value),
                RecentErrors = e.RecentErrors.ToList()
            });
        }

        if (snapshot.Endpoints.Count > 0)
            snapshot.AverageDurationMs = snapshot.Endpoints.Average(ep => ep.AverageDurationMs);

        return snapshot;
    }
}
