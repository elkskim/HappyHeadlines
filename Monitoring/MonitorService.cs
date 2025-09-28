using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.Trace;
using Serilog;
using OpenTelemetry;
using OpenTelemetry.Resources;

namespace Monitoring;

public static class MonitorService
{
    public static readonly string ServiceName = Assembly.GetCallingAssembly().GetName().Name ?? "Unknown";
    public static TracerProvider TracerProvider;
    public static ActivitySource ActivitySource = new ActivitySource(ServiceName);
    
    public static ILogger Log => Serilog.Log.Logger;
    
    static MonitorService()
    {
        TracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddConsoleExporter()
            .AddZipkinExporter()
            .AddSource(ActivitySource.Name)
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(ServiceName))
            .Build();
        
        Serilog.Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console()
            .CreateLogger();
    }
}