using SmsBridge.Abstractions;

namespace SmsBridge.Webhooks;

/// <summary>Parses Vonage delivery receipt webhook callbacks.</summary>
public sealed class VonageWebhookParser : ISmsWebhookParser
{
    public SmsProviderType Provider => SmsProviderType.Vonage;

    public SmsWebhookEvent Parse(IDictionary<string, string> payload)
    {
        payload.TryGetValue("messageId", out var messageId);
        payload.TryGetValue("msisdn", out var to);
        payload.TryGetValue("status", out var status);
        payload.TryGetValue("message-timestamp", out var tsStr);
        payload.TryGetValue("err-code", out var errorCode);

        DateTimeOffset.TryParse(tsStr, out var timestamp);

        return new SmsWebhookEvent
        {
            Provider = Provider,
            MessageId = messageId,
            To = to,
            Status = MapStatus(status),
            ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? null : errorCode,
            IsTransientFailure = IsTransientError(errorCode),
            Timestamp = timestamp == default ? DateTimeOffset.UtcNow : timestamp,
            Raw = new Dictionary<string, string>(payload)
        };
    }

    private static SmsDeliveryStatus MapStatus(string? status) => status?.ToUpperInvariant() switch
    {
        "DELIVERED" => SmsDeliveryStatus.Delivered,
        "BUFFERED" or "ACCEPTED" or "ACCEPTD" => SmsDeliveryStatus.Queued,
        "EXPIRED" or "FAILED" or "REJECTED" => SmsDeliveryStatus.Failed,
        _ => SmsDeliveryStatus.Unknown
    };

    private static bool IsTransientError(string? errorCode) => errorCode switch
    {
        "2" or "7" or "8" => true,
        _ => false
    };
}
