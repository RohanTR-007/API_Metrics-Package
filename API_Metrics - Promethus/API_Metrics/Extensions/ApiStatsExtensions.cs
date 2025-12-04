using ApiStats.Dashboard.Middleware;
using ApiStats.Dashboard.Options;
using ApiStats.Dashboard.Services;
using ApiStats.Dashboard.UI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;

namespace ApiStats.Dashboard;

public static class ApiStatsExtensions
{
    public static IServiceCollection AddApiStatsDashboard(
        this IServiceCollection services,
        Action<ApiStatsOptions>? configure = null)
    {
        var opts = new ApiStatsOptions();
        configure?.Invoke(opts);

        services.AddSingleton(opts);
        services.AddSingleton<ApiStatsStore>();

        return services;
    }

    public static IApplicationBuilder UseApiStats(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiStatsMiddleware>();
    }

    public static IEndpointRouteBuilder MapApiStatsDashboard(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/apiMetricDashboard", async context =>
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(DashboardHtml.Content);
        });

        endpoints.MapGet("/apiMetricDashboard-data", (ApiStatsStore store) =>
        {
            var snapshot = store.GetSnapshot();
            return Results.Json(snapshot);
        });

        return endpoints;
    }

    // optional helper: expose /metrics via Prometheus
    public static IEndpointRouteBuilder MapApiMetricsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapMetrics(); // from prometheus-net.AspNetCore
        return endpoints;
    }
}
