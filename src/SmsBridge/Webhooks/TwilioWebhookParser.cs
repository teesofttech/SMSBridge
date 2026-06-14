using SmsBridge.Abstractions;

namespace SmsBridge.Webhooks;

/// <summary>Parses Twilio delivery status webhook callbacks.</summary>
public sealed class TwilioWebhookParser : ISmsWebhookParser
{
    public SmsProviderType Provider => SmsProviderType.Twilio;

    public SmsWebhookEvent Parse(IDictionary<string, string> payload)
    {
        payload.TryGetValue("MessageSid", out var messageId);
        payload.TryGetValue("To", out var to);
        payload.TryGetValue("MessageStatus", out var status);

        return new SmsWebhookEvent
        {
            Provider = Provider,
            MessageId = messageId,
            To = to,
            Status = MapStatus(status),
            Timestamp = DateTimeOffset.UtcNow,
            Raw = new Dictionary<string, string>(payload)
        };
    }

    private static SmsDeliveryStatus MapStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "accepted" => SmsDeliveryStatus.Accepted,
        "queued" => SmsDeliveryStatus.Queued,
        "sending" or "sent" => SmsDeliveryStatus.Sent,
        "delivered" => SmsDeliveryStatus.Delivered,
        "undelivered" => SmsDeliveryStatus.Undelivered,
        "failed" => SmsDeliveryStatus.Failed,
        _ => SmsDeliveryStatus.Unknown
    };
}
