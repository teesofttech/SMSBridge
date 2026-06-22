using SmsBridge.Abstractions;

namespace SmsBridge.Webhooks;

/// <summary>Parses Plivo message status callback payloads.</summary>
public sealed class PlivoWebhookParser : ISmsWebhookParser
{
    public SmsProviderType Provider => SmsProviderType.Plivo;

    public SmsWebhookEvent Parse(IDictionary<string, string> payload)
    {
        payload.TryGetValue("MessageUUID", out var messageId);
        payload.TryGetValue("To", out var to);
        payload.TryGetValue("Status", out var status);
        payload.TryGetValue("ErrorCode", out var errorCode);

        return new SmsWebhookEvent
        {
            Provider = Provider,
            MessageId = messageId,
            To = to,
            Status = MapStatus(status),
            ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? null : errorCode,
            IsTransientFailure = IsTransientError(errorCode),
            Timestamp = DateTimeOffset.UtcNow,
            Raw = new Dictionary<string, string>(payload)
        };
    }

    private static SmsDeliveryStatus MapStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "queued" => SmsDeliveryStatus.Queued,
        "sent" => SmsDeliveryStatus.Sent,
        "delivered" or "read" => SmsDeliveryStatus.Delivered,
        "undelivered" => SmsDeliveryStatus.Undelivered,
        "failed" => SmsDeliveryStatus.Failed,
        _ => SmsDeliveryStatus.Unknown
    };

    private static bool IsTransientError(string? errorCode) => errorCode switch
    {
        // 20 = carrier network error, 80 = destination temporarily unavailable,
        // 300 = internal dispatch failure. Plivo explicitly recommends retrying these later.
        "20" or "80" or "300" => true,
        _ => false
    };
}
