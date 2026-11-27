using System.Diagnostics;
using System.Reflection;

namespace SmsBridge.Observability;

public static class SmsBridgeTelemetry
{
    public const string ActivitySourceName = "SmsBridge";
    public const string MeterName = "SmsBridge";

    [Obsolete("No longer updated. The SmsBridge ActivitySource version now follows the assembly informational version.")]
    public const string Version = "0.1.0";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName, GetAssemblyVersion());

    private static string GetAssemblyVersion()
    {
        var version = typeof(SmsBridgeTelemetry).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (string.IsNullOrWhiteSpace(version))
            return "0.0.0";

        // The SDK appends "+<commit>" build metadata; report the package version only.
        var metadataStart = version.IndexOf('+');
        return metadataStart >= 0 ? version[..metadataStart] : version;
    }

    internal static Activity? StartSendActivity(string provider)
    {
        var activity = ActivitySource.StartActivity("sms.send", ActivityKind.Client);
        activity?.SetTag("sms.provider", provider);
        return activity;
    }
}
