using API_Metrics.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using API_Metrics.Options;
using API_Metrics.Models;

namespace API_Metrics.Middleware
{
    public class ApiStatsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ApiStatsOptions _options;

        public ApiStatsMiddleware(RequestDelegate next, ApiStatsOptions options)
        {
            _next = next;
            _options = options;
        }

        public async Task InvokeAsync(HttpContext context, ApiStatsStore store)
        {
            var path = context.Request.Path.HasValue ? context.Request.Path.Value! : "/";
            // quick exclude checks
            if (ShouldExcludePath(path)) { await _next(context); return; }

            var sw = Stopwatch.StartNew();
            Exception? caught = null;

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                caught = ex;
                // ensure response code reflects server error so downstream metrics see it
                if (context.Response.HasStarted == false)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }
                // record now (we will also record in finally, but recording here ensures error is captured immediately)
                RecordException(context, store, ex, sw.ElapsedMilliseconds);
                // rethrow so the app's exception handler can do its job
                throw;
            }
            finally
            {
                sw.Stop();
                var endpointPath = context.Request.Path.Value ?? "unknown";

                if (!ShouldExcludePath(endpointPath))
                {
                    var statusCode = context.Response.StatusCode;
                    store.RecordRequest(endpointPath, statusCode, sw.ElapsedMilliseconds);
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

        private void RecordException(HttpContext context, ApiStatsStore store, Exception ex, long elapsedMs)
        {
            var endpoint = context.Request.Path.HasValue ? context.Request.Path.Value! : "unknown";

            var error = new ErrorRecord
            {
                Endpoint = endpoint,
                Method = context.Request.Method,
                Message = ex.Message,
                StackTrace = ex.StackTrace ?? string.Empty,
                MaskedQueryString = MaskQueryString(context.Request.QueryString.Value ?? ""),
                MaskedHeaders = MaskHeaders(context.Request.Headers)
            };

            store.RecordError(endpoint, error);
        }
        private string MaskQueryString(string qs)
        {
            if (string.IsNullOrEmpty(qs)) return "";
            // qs includes leading '?', remove and parse
            var raw = qs.StartsWith("?") ? qs.Substring(1) : qs;
            var parts = raw.Split('&', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                var kv = parts[i].Split('=', 2);
                if (kv.Length == 2 && _options.MaskQueryParameters.Contains(kv[0]))
                {
                    parts[i] = $"{kv[0]}=*****";
                }
            }
            return "?" + string.Join("&", parts);
        }

        private IDictionary<string, string> MaskHeaders(IHeaderDictionary headers)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var h in headers)
            {
                if (_options.MaskHeaders.Contains(h.Key))
                    dict[h.Key] = "*****";
                else
                    dict[h.Key] = h.Value.ToString();
            }
            return dict;
        }

    }
}
