using System.Text.Json;
using SmsBridge.Abstractions;

namespace SmsBridge.Providers.MessageBird;

internal static class MessageBirdSmsResponseMapper
{
    public static SmsSendResult FromResponse(string providerName, string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var messageId = ReadString(root, "id");
        if (!TryGetFirstRecipient(root, out var recipient))
            return SmsSendResult.Succeeded(providerName, messageId, SmsDeliveryStatus.Accepted);

        var status = ReadString(recipient, "status");
        var deliveryStatus = MapStatus(status);

        if (deliveryStatus is SmsDeliveryStatus.Failed or SmsDeliveryStatus.Undelivered)
        {
            var errorCode = ReadString(recipient, "statusErrorCode");
            var errorMessage = ReadString(recipient, "statusReason")
                ?? $"MessageBird status: {status}";

            return SmsSendResult.Failed(
                providerName,
                errorCode,
                errorMessage,
                status: deliveryStatus,
                mayHaveBeenAccepted: true);
        }

        return SmsSendResult.Succeeded(providerName, messageId, deliveryStatus);
    }

    public static SmsSendResult FromErrorResponse(
        string providerName,
        int httpStatusCode,
        string json)
    {
        string? errorCode = null;
        string? errorMessage = null;

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.TryGetProperty("errors", out var errors) &&
                errors.ValueKind == JsonValueKind.Array &&
                errors.GetArrayLength() > 0)
            {
                var error = errors[0];
                errorCode = ReadString(error, "code");
                errorMessage = ReadString(error, "description");
            }
        }
        catch (JsonException)
        {
            // Best-effort parsing; HTTP status still determines retry classification.
        }

        var isTransient = httpStatusCode == 429 || httpStatusCode >= 500;
        return SmsSendResult.Failed(
            providerName,
            errorCode,
            errorMessage ?? $"HTTP {httpStatusCode}",
            isTransient,
            mayHaveBeenAccepted: httpStatusCode >= 500);
    }

    private static bool TryGetFirstRecipient(JsonElement root, out JsonElement recipient)
    {
        recipient = default;

        if (!root.TryGetProperty("recipients", out var recipients) ||
            !recipients.TryGetProperty("items", out var items) ||
            items.ValueKind != JsonValueKind.Array ||
            items.GetArrayLength() == 0)
        {
            return false;
        }

        recipient = items[0];
        return true;
    }

private static SmsDeliveryStatus MapStatus(string? status) => status?.ToLowerInvariant() switch
{
    "scheduled" or "buffered" => SmsDeliveryStatus.Queued,
    "sent" => SmsDeliveryStatus.Sent,
    "delivered" => SmsDeliveryStatus.Delivered,
    "expired" => SmsDeliveryStatus.Undelivered,
    "delivery_failed" => SmsDeliveryStatus.Failed,
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
