using API_Metrics.Middleware;
using API_Metrics.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using API_Metrics.UI;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using API_Metrics.Options;

namespace API_Metrics.Extensions
{
    public static class ApiStatsExtensions
    {
        // Register services

        public static IServiceCollection AddApiStatsDashboard(this IServiceCollection services)
        {
            return services.AddApiStatsDashboard(_ => { });
        }

        public static IServiceCollection AddApiStatsDashboard(this IServiceCollection services, Action<ApiStatsOptions>? configure = null)
        {
            var opts = new ApiStatsOptions();
            configure?.Invoke(opts);
            services.AddSingleton(opts);
            services.AddSingleton<ApiStatsStore>();
            return services;
        }

        // Register middleware
        public static IApplicationBuilder UseApiStats(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ApiStatsMiddleware>();
        }

        // Map dashboard + data endpoints
        public static IEndpointRouteBuilder MapApiStatsDashboard(this IEndpointRouteBuilder endpoints)
        {
            // HTML dashboard
            endpoints.MapGet("/apiMetricDashboard", async context =>
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync(DashboardHtml.Content);
            });

            // JSON data endpoint


            endpoints.MapGet("/apiMetricDashboard-data", (ApiStatsStore store) =>
            {
                var snapshot = store.GetSnapshot();
                return Results.Json(snapshot);
            });


            return endpoints;
        }
    }
}
