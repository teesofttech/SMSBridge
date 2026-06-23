using System.Text.Json;
using SmsBridge.Abstractions;

namespace SmsBridge.Webhooks;

/// <summary>Parses Telnyx message lifecycle webhook payloads.</summary>
public sealed class TelnyxWebhookParser : ISmsWebhookParser
{
    public SmsProviderType Provider => SmsProviderType.Telnyx;

    public SmsWebhookEvent Parse(IDictionary<string, string> payload) =>
        throw new SmsBridgeException("Telnyx messaging webhooks use JSON payloads. Call ParseJson instead.");

    public SmsWebhookEvent ParseJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        var eventData = document.RootElement.GetProperty("data");
        var payload = eventData.GetProperty("payload");

        var messageId = payload.TryGetProperty("id", out var id) ? id.GetString() : null;
        var occurredAt = eventData.TryGetProperty("occurred_at", out var occurredAtElement)
            ? occurredAtElement.GetString()
            : null;

        string? to = null;
        string? status = null;
        if (payload.TryGetProperty("to", out var recipients) &&
            recipients.ValueKind == JsonValueKind.Array &&
            recipients.GetArrayLength() > 0)
        {
            var recipient = recipients[0];
            to = recipient.TryGetProperty("phone_number", out var number) ? number.GetString() : null;
            status = recipient.TryGetProperty("status", out var statusElement) ? statusElement.GetString() : null;
        }

        string? errorCode = null;
        if (payload.TryGetProperty("errors", out var errors) &&
            errors.ValueKind == JsonValueKind.Array &&
            errors.GetArrayLength() > 0)
        {
            var code = errors[0].GetProperty("code");
            errorCode = code.ValueKind == JsonValueKind.String
                ? code.GetString()
                : code.GetRawText();
        }

        return new SmsWebhookEvent
        {
            Provider = Provider,
            MessageId = messageId,
            To = to,
            Status = MapStatus(status),
            ErrorCode = errorCode,
            IsTransientFailure = errorCode is "40006" or "40008",
            Timestamp = DateTimeOffset.TryParse(occurredAt, out var parsed)
                ? parsed
                : DateTimeOffset.UtcNow,
            Raw = new Dictionary<string, string>()
        };
    }

    private static SmsDeliveryStatus MapStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "queued" => SmsDeliveryStatus.Queued,
        "sending" or "sent" => SmsDeliveryStatus.Sent,
        "delivered" => SmsDeliveryStatus.Delivered,
        "delivery_failed" => SmsDeliveryStatus.Failed,
        "delivery_unconfirmed" => SmsDeliveryStatus.Undelivered,
        _ => SmsDeliveryStatus.Unknown
    };
}
