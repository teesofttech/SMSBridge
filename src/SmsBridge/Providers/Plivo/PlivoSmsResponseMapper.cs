using System.Text.Json;
using SmsBridge.Abstractions;
using SmsBridge.Internal;

namespace SmsBridge.Providers.Plivo;

internal static class PlivoSmsResponseMapper
{
    public static SmsSendResult FromResponse(string providerName, string json)
    {
        try
        {
            return ParseResponse(providerName, json);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            return ProviderResponseFailures.InvalidSuccessResponse(providerName, "Plivo");
        }
    }

    private static SmsSendResult ParseResponse(string providerName, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("message_uuid", out var uuidEl) ||
            uuidEl.ValueKind != JsonValueKind.Array ||
            uuidEl.GetArrayLength() == 0)
        {
            return ProviderResponseFailures.InvalidSuccessResponse(providerName, "Plivo");
        }

        var messageId = uuidEl[0].GetString();
        if (string.IsNullOrWhiteSpace(messageId))
            return ProviderResponseFailures.InvalidSuccessResponse(providerName, "Plivo");

        return SmsSendResult.Succeeded(providerName, messageId, SmsDeliveryStatus.Queued);
    }

    public static SmsSendResult FromErrorResponse(string providerName, int httpStatusCode, string json)
    {
        string? errorMessage = null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            errorMessage = root.TryGetProperty("error", out var errEl) ? errEl.GetString() : null;
        }
        catch (JsonException) { /* best-effort */ }

        bool isTransient = httpStatusCode >= 500 || httpStatusCode == 429;
        return SmsSendResult.Failed(
            providerName,
            null,
            errorMessage ?? $"HTTP {httpStatusCode}",
            isTransient,
            mayHaveBeenAccepted: httpStatusCode >= 500);
    }
}
