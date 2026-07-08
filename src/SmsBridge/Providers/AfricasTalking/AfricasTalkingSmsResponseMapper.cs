using System.Globalization;
using System.Text.Json;
using SmsBridge.Abstractions;

namespace SmsBridge.Providers.AfricasTalking;

internal static class AfricasTalkingSmsResponseMapper
{
    public static SmsSendResult FromResponse(string providerName, string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!TryGetFirstRecipient(root, out var recipient))
                return SmsSendResult.Failed(providerName, null, "Unexpected response format", isTransient: false);

            var statusCode = ReadInt32(recipient, "statusCode");
            var messageId = ReadString(recipient, "messageId");
            var status = ReadString(recipient, "status");
            var deliveryStatus = MapDeliveryStatus(statusCode);

            if (statusCode is 100 or 101 or 102)
                return SmsSendResult.Succeeded(providerName, messageId, deliveryStatus);

            var errorCode = statusCode?.ToString(CultureInfo.InvariantCulture);
            var errorMessage = status ?? $"Africa's Talking statusCode: {errorCode ?? "unknown"}";
            return SmsSendResult.Failed(
                providerName,
                errorCode,
                errorMessage,
                isTransient: IsTransientStatusCode(statusCode),
                status: deliveryStatus,
                mayHaveBeenAccepted: IsTransientStatusCode(statusCode));
        }
        catch (JsonException)
        {
            return SmsSendResult.Failed(
                providerName,
                null,
                "Unable to parse Africa's Talking response.",
                isTransient: false);
        }
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

            errorCode = ReadString(root, "code") ?? ReadString(root, "errorCode");
            errorMessage = ReadString(root, "message")
                ?? ReadString(root, "errorMessage")
                ?? ReadString(root, "error");
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

        if (!root.TryGetProperty("SMSMessageData", out var messageData) ||
            !messageData.TryGetProperty("Recipients", out var recipients) ||
            recipients.ValueKind != JsonValueKind.Array ||
            recipients.GetArrayLength() == 0)
        {
            return false;
        }

        recipient = recipients[0];
        return true;
    }

    private static SmsDeliveryStatus MapDeliveryStatus(int? statusCode) => statusCode switch
    {
        100 => SmsDeliveryStatus.Accepted,
        101 => SmsDeliveryStatus.Sent,
        102 => SmsDeliveryStatus.Queued,
        403 or 404 or 409 => SmsDeliveryStatus.Undelivered,
        401 or 402 or 405 or 406 or 407 or 500 or 501 or 502 => SmsDeliveryStatus.Failed,
        _ => SmsDeliveryStatus.Unknown
    };

    private static bool IsTransientStatusCode(int? statusCode) => statusCode is 500 or 501 or 502;

    private static int? ReadInt32(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return null;

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value))
            return value;

        if (property.ValueKind == JsonValueKind.String &&
            int.TryParse(
                property.GetString(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out value))
        {
            return value;
        }

        return null;
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return null;

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Array or JsonValueKind.Object => property.GetRawText(),
            _ => null
        };
    }
}
