using System.Text.Json;
using SmsBridge.Abstractions;

namespace SmsBridge.Providers.Plivo;

internal static class PlivoSmsResponseMapper
{
    public static SmsSendResult FromResponse(string providerName, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var messageId = root.TryGetProperty("message_uuid", out var uuidEl) &&
                        uuidEl.ValueKind == JsonValueKind.Array &&
                        uuidEl.GetArrayLength() > 0
            ? uuidEl[0].GetString()
            : null;

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
        return SmsSendResult.Failed(providerName, null, errorMessage ?? $"HTTP {httpStatusCode}", isTransient);
    }
}
