using System.Diagnostics;
using System.Diagnostics.Metrics;
using ApiStats.Dashboard.Models;
using ApiStats.Dashboard.Options;
using ApiStats.Dashboard.Services;
using Microsoft.AspNetCore.Http;
using Prometheus;

namespace ApiStats.Dashboard.Middleware;

public class ApiStatsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApiStatsOptions _options;

    // Prometheus metrics (labels: endpoint, method, status_code)
    private static readonly Counter RequestCounter = Metrics.CreateCounter(
        "apistats_http_requests_total",
        "Total HTTP requests counted by ApiStats.Dashboard.",
        new[] { "endpoint", "method", "status_code" });

    private static readonly Histogram RequestDuration = Metrics.CreateHistogram(
        "apistats_http_request_duration_seconds",
        "HTTP request duration in seconds counted by ApiStats.Dashboard.",
        new[] { "endpoint", "method" });

    public ApiStatsMiddleware(RequestDelegate next, ApiStatsOptions options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context, ApiStatsStore store)
    {
        var path = context.Request.Path.HasValue ? context.Request.Path.Value! : "/";
        if (ShouldExcludePath(path))
        {
            await _next(context);
            return;
        }

        var method = context.Request.Method ?? "UNKNOWN";
        var sw = Stopwatch.StartNew();
        Exception? caught = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            caught = ex;

            if (!context.Response.HasStarted)
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var errorRecord = BuildErrorRecord(context, ex);
            store.RecordError(errorRecord);
            throw;
        }
        finally
        {
            sw.Stop();
            var elapsedMs = sw.ElapsedMilliseconds;
            var statusCode = context.Response.StatusCode;

            // in-memory stats (for dashboard)
            store.RecordRequest(path, method, statusCode, elapsedMs);

            // Prometheus metrics
            if (_options.EnablePrometheus)
            {
                var statusLabel = statusCode.ToString();
                RequestCounter.WithLabels(path, method, statusLabel).Inc();
                RequestDuration.WithLabels(path, method).Observe(elapsedMs / 1000.0);
            }
        }
    }

    private bool ShouldExcludePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        if (_options.ExcludedPaths.Contains(path)) return true;
        foreach (var prefix in _options.ExcludedPathPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private ErrorRecord BuildErrorRecord(HttpContext context, Exception ex)
    {
        return new ErrorRecord
        {
            Endpoint = context.Request.Path.HasValue ? context.Request.Path.Value! : "unknown",
            Method = context.Request.Method ?? "UNKNOWN",
            Message = ex.Message,
            StackTrace = ex.StackTrace ?? string.Empty,
            MaskedQueryString = MaskQueryString(context.Request.QueryString.Value ?? ""),
            MaskedHeaders = MaskHeaders(context.Request.Headers)
        };
    }

    private string MaskQueryString(string qs)
    {
        if (string.IsNullOrEmpty(qs)) return "";
        var raw = qs.StartsWith("?") ? qs[1..] : qs;
        var parts = raw.Split('&', StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < parts.Length; i++)
        {
            var kv = parts[i].Split('=', 2);
            if (kv.Length == 2 && _options.MaskQueryParameters.Contains(kv[0]))
                parts[i] = $"{kv[0]}=*****";
        }

        return parts.Length == 0 ? "" : "?" + string.Join("&", parts);
    }

    private IDictionary<string, string> MaskHeaders(IHeaderDictionary headers)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var h in headers)
        {
            dict[h.Key] = _options.MaskHeaders.Contains(h.Key)
                ? "*****"
                : h.Value.ToString();
        }
        return dict;
    }
}
