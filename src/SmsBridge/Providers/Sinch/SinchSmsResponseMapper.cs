using System.Text.Json;
using SmsBridge.Abstractions;

namespace SmsBridge.Providers.Sinch;

internal static class SinchSmsResponseMapper
{
    public static SmsSendResult FromResponse(string providerName, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var id = root.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;

        // A successful POST creates a batch. Delivery outcomes are reported separately.
        return SmsSendResult.Succeeded(providerName, id, SmsDeliveryStatus.Queued);
    }

    public static SmsSendResult FromErrorResponse(string providerName, int httpStatusCode, string json)
    {
        string? errorCode = null;
        string? errorMessage = null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            errorCode = root.TryGetProperty("code", out var codeEl) ? codeEl.GetString() : null;
            errorMessage = root.TryGetProperty("text", out var textEl) ? textEl.GetString() : null;
        }
        catch (JsonException) { /* best-effort */ }

        bool isTransient = httpStatusCode >= 500 || httpStatusCode == 429;
        return SmsSendResult.Failed(
            providerName,
            errorCode,
            errorMessage ?? $"HTTP {httpStatusCode}",
            isTransient,
            mayHaveBeenAccepted: httpStatusCode >= 500);
    }
}
