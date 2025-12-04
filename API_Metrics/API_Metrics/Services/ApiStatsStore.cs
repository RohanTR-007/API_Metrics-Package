using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using API_Metrics.Models;
using API_Metrics.Options;

namespace API_Metrics.Services
{

    public class ApiStatsStore
    {
        private readonly ConcurrentDictionary<string, EndpointStats> _endpoints = new();
        private readonly ApiStatsOptions _options;

        public ApiStatsStore(ApiStatsOptions options)
        {
            _options = options;
        }
        public void RecordRequest(string endpoint, int statusCode, long durationMs)
        {
            var stats = _endpoints.GetOrAdd(endpoint, ep => new EndpointStats(ep));
            stats.Record(statusCode, durationMs);
        }

        public void RecordError(string endpoint, ErrorRecord err)
        {
            var stats = _endpoints.GetOrAdd(endpoint, ep => new EndpointStats(ep, _options.MaxErrorRecordsPerEndpoint));
            stats.RecordError(err);
        }

        //public ApiStatsSnapshot GetSnapshot()
        //{
        //    var snapshot = new ApiStatsSnapshot();

        //    foreach (var kvp in _endpoints)
        //    {
        //        var e = kvp.Value;

        //        snapshot.TotalRequests += e.TotalRequests;
        //        snapshot.TotalSuccess += e.SuccessRequests;
        //        snapshot.TotalFailed += e.FailedRequests;
        //        snapshot.AverageDurationMs += e.AverageDurationMs; // We'll normalize later if needed

        //        snapshot.Endpoints.Add(new EndpointStatsSnapshot
        //        {
        //            Endpoint = e.Endpoint,
        //            TotalRequests = e.TotalRequests,
        //            SuccessRequests = e.SuccessRequests,
        //            FailedRequests = e.FailedRequests,
        //            AverageDurationMs = e.AverageDurationMs,
        //            StatusCodeCounts = e.StatusCodeCounts.ToDictionary(x => x.Key, x => x.Value)
        //        });
        //    }

        //    if (snapshot.Endpoints.Count > 0)
        //    {
        //        snapshot.AverageDurationMs /= snapshot.Endpoints.Count;
        //    }

        //    return snapshot;
        //}

        // snapshot: include RecentErrors per endpoint
        public ApiStatsSnapshot GetSnapshot()
        {
            var snapshot = new ApiStatsSnapshot();
            foreach (var kvp in _endpoints)
            {
                var e = kvp.Value;
                snapshot.TotalRequests += e.TotalRequests;
                snapshot.TotalSuccess += e.SuccessRequests;
                snapshot.TotalFailed += e.FailedRequests;
                snapshot.Endpoints.Add(new EndpointStatsSnapshot
                {
                    Endpoint = e.Endpoint,
                    TotalRequests = e.TotalRequests,
                    SuccessRequests = e.SuccessRequests,
                    FailedRequests = e.FailedRequests,
                    AverageDurationMs = e.AverageDurationMs,
                    StatusCodeCounts = e.StatusCodeCounts.ToDictionary(x => x.Key, x => x.Value),
                    RecentErrors = e.RecentErrors.ToList() // new field in snapshot
                });
            }
            if (snapshot.Endpoints.Count > 0)
                snapshot.AverageDurationMs = snapshot.Endpoints.Average(ep => ep.AverageDurationMs);
            return snapshot;
        }
    }

}
