using System.Text.Json;
using SmsBridge.Abstractions;

namespace SmsBridge.Webhooks;

/// <summary>Parses Sinch per-recipient delivery report callbacks.</summary>
public sealed class SinchWebhookParser : ISmsWebhookParser
{
    public SmsProviderType Provider => SmsProviderType.Sinch;

    public SmsWebhookEvent Parse(IDictionary<string, string> payload) =>
        throw new SmsBridgeException("Sinch delivery reports use JSON payloads. Call ParseJson instead.");

    public SmsWebhookEvent ParseJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var messageId = ReadString(root, "batch_id");
        var to = ReadString(root, "recipient");
        var status = ReadString(root, "status");
        var errorCode = ReadString(root, "code");
        var timestamp = ReadString(root, "at");

        return new SmsWebhookEvent
        {
            Provider = Provider,
            MessageId = messageId,
            To = to,
            Status = MapStatus(status),
            ErrorCode = errorCode,
            Timestamp = DateTimeOffset.TryParse(timestamp, out var parsed)
                ? parsed
                : DateTimeOffset.UtcNow,
            Raw = new Dictionary<string, string>()
        };
    }

    private static SmsDeliveryStatus MapStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "delivered" => SmsDeliveryStatus.Delivered,
        "pending" => SmsDeliveryStatus.Queued,
        "failed" => SmsDeliveryStatus.Failed,
        "expired" => SmsDeliveryStatus.Undelivered,
        _ => SmsDeliveryStatus.Unknown
    };

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return null;

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            _ => null
        };
    }
}
