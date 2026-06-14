using System.Diagnostics;

namespace SmsBridge.Observability;

public static class SmsBridgeTelemetry
{
    public const string ActivitySourceName = "SmsBridge";
    public const string MeterName = "SmsBridge";
    public const string Version = "0.1.0";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName, Version);

    internal static Activity? StartSendActivity(string provider, string to)
    {
        var activity = ActivitySource.StartActivity("sms.send", ActivityKind.Client);
        activity?.SetTag("sms.provider", provider);
        activity?.SetTag("sms.to.hash", HashPhone(to));
        return activity;
    }

    private static string HashPhone(string phone)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(phone));
        return Convert.ToHexString(bytes)[..8];
    }
}
