using Serilog.Core;
using Serilog.Events;

public class SerilogEnricher : ILogEventEnricher
{
    private readonly string _ApplicationId;
    private readonly string _ApplicationName;
    private readonly string _EnvironmentName;

    public SerilogEnricher(string applicationId, string applicationName, string environmentName)
    {
        _ApplicationId = applicationId;
        _ApplicationName = applicationName;
        _EnvironmentName = environmentName;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ApplicationId", _ApplicationId));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ApplicationName", _ApplicationName));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Environment", _EnvironmentName));
    }
}