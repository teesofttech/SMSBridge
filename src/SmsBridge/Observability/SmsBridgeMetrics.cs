namespace SmsBridge.Observability;

/// <summary>
/// Metric names for OpenTelemetry instrumentation.
/// Full metrics implementation is planned for a future release.
/// </summary>
public static class SmsBridgeMetrics
{
    public const string SmsSentCount = "smsbridge.sms.sent";
    public const string SmsFailedCount = "smsbridge.sms.failed";
    public const string SmsFailoverCount = "smsbridge.sms.failover";
    public const string SmsDurationMs = "smsbridge.sms.duration_ms";
}
