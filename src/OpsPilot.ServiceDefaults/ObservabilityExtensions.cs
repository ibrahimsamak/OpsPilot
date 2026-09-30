using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OpsPilot.ServiceDefaults;
public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder, params string[] extraSources)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        var otel = builder.Services.AddOpenTelemetry()
                    .ConfigureResource(resource => resource.AddService(serviceName: builder.Environment.ApplicationName))
                    .WithTracing(tracing=>
                    {
                        tracing
                        .AddAspNetCoreInstrumentation(o=>o.Filter = ctx=> !ctx.Request.Path.StartsWithSegments("/health"))
                        .AddHttpClientInstrumentation()
                        .AddNpgsql();
                        foreach(var source in extraSources) tracing.AddSource(source);
                    })
                    .WithMetrics(metrics =>
                    {
                        metrics
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation();
                        foreach (var source in extraSources) metrics.AddMeter(source);
                 });
        // Local: Aspire Dashboard (set by launchSettings.json).
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
            otel.UseOtlpExporter();

        // Cloud: Application Insights (set in user secrets / app settings).
        var appInsights = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        if (!string.IsNullOrWhiteSpace(appInsights))
            otel.UseAzureMonitor(o => o.ConnectionString = appInsights);

        return builder;
    }
}