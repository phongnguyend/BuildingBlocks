using DddDotNet.Infrastructure.Monitoring.AzureApplicationInsights;
using DddDotNet.Infrastructure.Monitoring.OpenTelemetry;

namespace DddDotNet.Infrastructure.Monitoring;

public class MonitoringOptions
{
    public AzureApplicationInsightsOptions AzureApplicationInsights { get; set; }

    public OpenTelemetryOptions OpenTelemetry { get; set; }
}
