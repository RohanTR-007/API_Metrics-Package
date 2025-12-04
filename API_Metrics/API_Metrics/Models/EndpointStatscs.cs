using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API_Metrics.Models
{
    public class EndpointStats
    {
        public string Endpoint { get; }
        public long TotalRequests => _totalRequests;
        public long SuccessRequests => _successRequests;
        public long FailedRequests => _failedRequests;
        public double AverageDurationMs => _totalRequests == 0 ? 0 : (double)_totalDurationMs / _totalRequests;

        private long _totalRequests;
        private long _successRequests;
        private long _failedRequests;
        private long _totalDurationMs;

        // store recent errors (bounded)
        private readonly ConcurrentQueue<ErrorRecord> _recentErrors = new();
        private readonly int _maxErrors;

        // statusCode -> count
        public ConcurrentDictionary<int, long> StatusCodeCounts { get; } = new();

        public EndpointStats(string endpoint, int maxErrors = 50)
        {
            Endpoint = endpoint;
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

            StatusCodeCounts.AddOrUpdate(
                statusCode,
                1,
                (_, oldValue) => oldValue + 1
            );
        }

        public void RecordError(ErrorRecord error)
        {
            _recentErrors.Enqueue(error);
            while (_recentErrors.Count > _maxErrors && _recentErrors.TryDequeue(out _)) { }
        }

        public IReadOnlyList<ErrorRecord> RecentErrors => _recentErrors.ToArray();
    }

}
