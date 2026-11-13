using System.Diagnostics;

namespace SmsBridge.Observability;

public static class SmsBridgeTelemetry
{
    public const string ActivitySourceName = "SmsBridge";
    public const string MeterName = "SmsBridge";
    public const string Version = "0.1.0";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName, Version);

    internal static Activity? StartSendActivity(string provider)
    {
        var activity = ActivitySource.StartActivity("sms.send", ActivityKind.Client);
        activity?.SetTag("sms.provider", provider);
        return activity;
    }
}
