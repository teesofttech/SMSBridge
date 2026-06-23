using System.Text.Json;
using SmsBridge.Abstractions;

namespace SmsBridge.Providers.Infobip;

internal static class InfobipSmsResponseMapper
{
    public static SmsSendResult FromResponse(string providerName, string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!root.TryGetProperty("messages", out var messages) ||
            messages.ValueKind != JsonValueKind.Array ||
            messages.GetArrayLength() == 0)
        {
            return SmsSendResult.Failed(
                providerName,
                null,
                "Infobip returned an empty messages array.");
        }

        var message = messages[0];
        var messageId = ReadString(message, "messageId");

        if (!message.TryGetProperty("status", out var status))
            return SmsSendResult.Succeeded(providerName, messageId, SmsDeliveryStatus.Accepted);

        var groupName = ReadString(status, "groupName");
        var statusId = ReadString(status, "id");
        var statusName = ReadString(status, "name");
        var description = ReadString(status, "description");
        var deliveryStatus = MapStatus(groupName);

        if (deliveryStatus is SmsDeliveryStatus.Failed or SmsDeliveryStatus.Undelivered)
        {
            return SmsSendResult.Failed(
                providerName,
                statusId ?? statusName,
                description ?? $"Infobip status: {groupName}",
                status: deliveryStatus);
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

            errorCode = ReadString(root, "errorCode");
            errorMessage = ReadString(root, "description");

            // The superseded v2 endpoint used this nested error format. Supporting it
            // keeps error parsing robust for account-specific gateway behavior.
            if (root.TryGetProperty("requestError", out var requestError) &&
                requestError.TryGetProperty("serviceException", out var serviceException))
            {
                errorCode ??= ReadString(serviceException, "messageId");
                errorMessage ??= ReadString(serviceException, "text");
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

    private static SmsDeliveryStatus MapStatus(string? groupName) =>
        groupName?.ToUpperInvariant() switch
        {
            "PENDING" => SmsDeliveryStatus.Queued,
            "DELIVERED" => SmsDeliveryStatus.Delivered,
            "EXPIRED" or "UNDELIVERABLE" => SmsDeliveryStatus.Undelivered,
            "REJECTED" => SmsDeliveryStatus.Failed,
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
