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
        var status = root.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null;

        var deliveryStatus = MapDeliveryStatus(status);

        if (deliveryStatus == SmsDeliveryStatus.Failed)
            return SmsSendResult.Failed(providerName, null, $"Sinch status: {status}", isTransient: false, status: deliveryStatus);

        return SmsSendResult.Succeeded(providerName, id, deliveryStatus);
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
        return SmsSendResult.Failed(providerName, errorCode, errorMessage ?? $"HTTP {httpStatusCode}", isTransient);
    }

    private static SmsDeliveryStatus MapDeliveryStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "in_progress" or "in progress" => SmsDeliveryStatus.Queued,
        "successful" => SmsDeliveryStatus.Delivered,
        "with_failures" or "failed" => SmsDeliveryStatus.Failed,
        _ => SmsDeliveryStatus.Unknown
    };
}
