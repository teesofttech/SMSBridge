using System.Text.Json;
using SmsBridge.Abstractions;
using SmsBridge.Internal;

namespace SmsBridge.Providers.Termii;

internal static class TermiiSmsResponseMapper
{
    public static SmsSendResult FromResponse(string providerName, string json)
    {
        try
        {
            return ParseResponse(providerName, json);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            return ProviderResponseFailures.InvalidSuccessResponse(providerName, "Termii");
        }
    }

    private static SmsSendResult ParseResponse(string providerName, string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var code = ReadString(root, "code");
        var messageId = ReadString(root, "message_id")
            ?? ReadString(root, "message_id_str");
        var message = ReadString(root, "message")
            ?? ReadString(root, "error")
            ?? ReadString(root, "errors");

        if (string.Equals(code, "ok", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(message, "Successfully Sent", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(messageId)
                ? ProviderResponseFailures.InvalidSuccessResponse(providerName, "Termii")
                : SmsSendResult.Succeeded(providerName, messageId, SmsDeliveryStatus.Accepted);
        }

        if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(message))
            return ProviderResponseFailures.InvalidSuccessResponse(providerName, "Termii");

        return SmsSendResult.Failed(
            providerName,
            code,
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

            errorCode = ReadString(root, "code") ?? ReadString(root, "error_code");
            errorMessage = ReadString(root, "message")
                ?? ReadString(root, "error")
                ?? ReadString(root, "errors");
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
        string.Equals(code, "timeout", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(code, "server_error", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(code, "internal_server_error", StringComparison.OrdinalIgnoreCase);

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
