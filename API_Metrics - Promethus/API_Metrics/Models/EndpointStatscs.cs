using System.Collections.Concurrent;
using System.Threading;

namespace ApiStats.Dashboard.Models;

public class EndpointStats
{
    private readonly object _samplesLock = new();
    private readonly int _maxSamples;
    private readonly int _maxErrors;

    // rolling window for durations
    private readonly Queue<long> _durationsMs = new();
    private readonly ConcurrentQueue<ErrorRecord> _recentErrors = new();

    public string Endpoint { get; }
    public string Method { get; }

    private long _totalRequests;
    private long _successRequests;
    private long _failedRequests;
    private long _totalDurationMs;

    public long TotalRequests => _totalRequests;
    public long SuccessRequests => _successRequests;
    public long FailedRequests => _failedRequests;
    public double AverageDurationMs => _totalRequests == 0 ? 0 : (double)_totalDurationMs / _totalRequests;

    public ConcurrentDictionary<int, long> StatusCodeCounts { get; } = new();

    public EndpointStats(string endpoint, string method, int maxSamples, int maxErrors)
    {
        Endpoint = endpoint;
        Method = method;
        _maxSamples = maxSamples;
        _maxErrors = maxErrors;
    }

    public void Record(int statusCode, long durationMs)
    {
        Interlocked.Increment(ref _totalRequests);
        Interlocked.Add(ref _totalDurationMs, durationMs);

        if (statusCode >= 200 && statusCode < 400)
            Interlocked.Increment(ref _successRequests);
        else
            Interlocked.Increment(ref _failedRequests);

        StatusCodeCounts.AddOrUpdate(statusCode, 1, (_, old) => old + 1);

        lock (_samplesLock)
        {
            _durationsMs.Enqueue(durationMs);
            while (_durationsMs.Count > _maxSamples)
                _durationsMs.Dequeue();
        }
    }

    public void RecordError(ErrorRecord error)
    {
        _recentErrors.Enqueue(error);
        while (_recentErrors.Count > _maxErrors && _recentErrors.TryDequeue(out _)) { }
    }

    public IReadOnlyList<ErrorRecord> RecentErrors => _recentErrors.ToArray();

    public (double p50, double p95, double p99) ComputePercentiles()
    {
        long[] snapshot;
        lock (_samplesLock)
        {
            if (_durationsMs.Count == 0) return (0, 0, 0);
            snapshot = _durationsMs.ToArray();
        }

        Array.Sort(snapshot);
        double P(double p)
        {
            if (snapshot.Length == 0) return 0;
            var idx = (int)Math.Ceiling(p * snapshot.Length) - 1;
            idx = Math.Clamp(idx, 0, snapshot.Length - 1);
            return snapshot[idx];
        }

        return (P(0.50), P(0.95), P(0.99));
    }
}
