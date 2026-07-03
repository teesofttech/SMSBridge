using System.Text.Json;
using SmsBridge.Abstractions;

namespace SmsBridge.Providers.SmartSms;

internal static class SmartSmsSmsResponseMapper
{
    public static SmsSendResult FromResponse(string providerName, string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var code = ReadString(root, "code");
        var message = ReadString(root, "message")
            ?? ReadString(root, "comment")
            ?? ReadFirstErrorTitle(root);
        var messageId = ReadString(root, "msg_id")
            ?? ReadString(root, "msgId")
            ?? ReadString(root, "message_id");
        var success = ReadBoolean(root, "success");

        if (code == "1000" || success == true)
            return SmsSendResult.Succeeded(providerName, messageId, SmsDeliveryStatus.Accepted);

        var errorCode = code ?? (success == false ? "success:false" : null);
        return SmsSendResult.Failed(
            providerName,
            errorCode,
            message ?? "Unexpected response format",
            isTransient: IsTransientCode(code),
            mayHaveBeenAccepted: IsTransientCode(code));
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
                ?? ReadString(root, "comment")
                ?? ReadString(root, "error")
                ?? ReadFirstErrorTitle(root);
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

    private static bool IsTransientCode(string? code) =>
        int.TryParse(code, out var value) && value >= 5000;

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

    private static string? ReadFirstErrorTitle(JsonElement element)
    {
        if (!element.TryGetProperty("errors", out var errors) ||
            errors.ValueKind != JsonValueKind.Array ||
            errors.GetArrayLength() == 0)
        {
            return null;
        }

        var first = errors[0];
        return ReadString(first, "title");
    }
}
