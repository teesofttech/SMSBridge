using System.Text.Json;
using SmsBridge.Abstractions;

namespace SmsBridge.Providers.Unifonic;

internal static class UnifonicSmsResponseMapper
{
    public static SmsSendResult FromResponse(string providerName, string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var success = ReadBoolean(root, "Success") ?? ReadBoolean(root, "success");
            var messageId = ReadString(root, "MessageID")
                ?? ReadString(root, "messageId")
                ?? ReadString(root, "MessageId");
            var status = ReadString(root, "Status") ?? ReadString(root, "status");
            var errorCode = ReadString(root, "ErrorCode")
                ?? ReadString(root, "errorCode")
                ?? ReadString(root, "Code")
                ?? ReadString(root, "code");
            var errorMessage = ReadString(root, "Message")
                ?? ReadString(root, "message")
                ?? ReadString(root, "Error")
                ?? ReadString(root, "error");

            if (success == true || IsAcceptedStatus(status))
                return SmsSendResult.Succeeded(providerName, messageId, MapStatus(status));

            return SmsSendResult.Failed(
                providerName,
                errorCode ?? (success == false ? "Success:false" : status),
                errorMessage ?? status ?? "Unexpected response format",
                isTransient: IsTransientStatus(status) || IsTransientErrorCode(errorCode),
                mayHaveBeenAccepted: IsTransientStatus(status) || IsTransientErrorCode(errorCode));
        }
        catch (JsonException)
        {
            return SmsSendResult.Failed(
                providerName,
                null,
                "Unable to parse Unifonic response.",
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

            errorCode = ReadString(root, "ErrorCode")
                ?? ReadString(root, "errorCode")
                ?? ReadString(root, "Code")
                ?? ReadString(root, "code");
            errorMessage = ReadString(root, "Message")
                ?? ReadString(root, "message")
                ?? ReadString(root, "Error")
                ?? ReadString(root, "error");
        }
        catch (JsonException)
        {
            // Best-effort parsing; HTTP status still drives retry classification.
        }

        var isTransient = httpStatusCode == 429 ||
            httpStatusCode >= 500 ||
            IsTransientErrorCode(errorCode);

        return SmsSendResult.Failed(
            providerName,
            errorCode,
            errorMessage ?? $"HTTP {httpStatusCode}",
            isTransient,
            mayHaveBeenAccepted: httpStatusCode >= 500);
    }

    private static bool IsAcceptedStatus(string? status) =>
        status?.Equals("Sent", StringComparison.OrdinalIgnoreCase) == true ||
        status?.Equals("Queued", StringComparison.OrdinalIgnoreCase) == true ||
        status?.Equals("Accepted", StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsTransientStatus(string? status) =>
        status?.Equals("Pending", StringComparison.OrdinalIgnoreCase) == true ||
        status?.Equals("Processing", StringComparison.OrdinalIgnoreCase) == true ||
        status?.Equals("Retrying", StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsTransientErrorCode(string? errorCode) =>
        errorCode is not null &&
        (errorCode.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
         errorCode.Contains("throttl", StringComparison.OrdinalIgnoreCase) ||
         errorCode.Contains("rate", StringComparison.OrdinalIgnoreCase));

    private static SmsDeliveryStatus MapStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "sent" => SmsDeliveryStatus.Sent,
        "queued" => SmsDeliveryStatus.Queued,
        "accepted" => SmsDeliveryStatus.Accepted,
        _ => SmsDeliveryStatus.Accepted
    };

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
            _ => null
        };
    }

    private static bool? ReadBoolean(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return null;

        if (property.ValueKind is JsonValueKind.True or JsonValueKind.False)
            return property.GetBoolean();

        if (property.ValueKind == JsonValueKind.String &&
            bool.TryParse(property.GetString(), out var value))
        {
            return value;
        }

        return null;
    }
}
