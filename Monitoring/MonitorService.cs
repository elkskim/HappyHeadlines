using System.Diagnostics;
using System.Reflection;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Monitoring;

public static class MonitorService
{
    
        public static TracerProvider? TracerProvider;
        public static ActivitySource? ActivitySource;
        public static ILogger Log => Serilog.Log.Logger;

        public static void Initialize(string serviceName)
        {
            ActivitySource = new ActivitySource(serviceName);

            TracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddZipkinExporter(o => o.Endpoint = new Uri("http://zipkin:9411/api/v2/spans"))
                .AddConsoleExporter()
                .AddSource(serviceName)
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .Build();

            Log.Information("Tracer provider initialized for {ServiceName}", serviceName);
        }

        public static void ConfigureSerilog(HostBuilderContext context, IServiceProvider services, LoggerConfiguration config, string serviceName)
        {
            config
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", serviceName)
                .WriteTo.Seq("http://seq:80")
                .WriteTo.Console();
        }
    

    
}