using System.Text.Json;
using SmsBridge.Abstractions;

namespace SmsBridge.Providers.Telnyx;

internal static class TelnyxSmsResponseMapper
{
    public static SmsSendResult FromResponse(string providerName, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("data", out var data))
            return SmsSendResult.Failed(providerName, null, "Unexpected response format", isTransient: false);

        var id = data.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;

        string? status = null;
        if (data.TryGetProperty("to", out var toEl) &&
            toEl.ValueKind == JsonValueKind.Array &&
            toEl.GetArrayLength() > 0)
        {
            toEl[0].TryGetProperty("status", out var statusEl);
            status = statusEl.GetString();
        }

        var deliveryStatus = MapDeliveryStatus(status);

        if (deliveryStatus == SmsDeliveryStatus.Failed)
            return SmsSendResult.Failed(providerName, null, $"Telnyx status: {status}", isTransient: false, status: deliveryStatus);

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

            if (root.TryGetProperty("errors", out var errorsEl) &&
                errorsEl.ValueKind == JsonValueKind.Array &&
                errorsEl.GetArrayLength() > 0)
            {
                var first = errorsEl[0];
                errorCode = first.TryGetProperty("code", out var codeEl) ? codeEl.GetString() : null;
                errorMessage = first.TryGetProperty("detail", out var detailEl) ? detailEl.GetString() : null;
            }
        }
        catch (JsonException) { /* best-effort */ }

        bool isTransient = httpStatusCode >= 500 || httpStatusCode == 429;
        return SmsSendResult.Failed(providerName, errorCode, errorMessage ?? $"HTTP {httpStatusCode}", isTransient);
    }

    private static SmsDeliveryStatus MapDeliveryStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "queued" => SmsDeliveryStatus.Queued,
        "sending" or "sent" => SmsDeliveryStatus.Sent,
        "delivered" => SmsDeliveryStatus.Delivered,
        "failed" or "delivery_failed" => SmsDeliveryStatus.Failed,
        _ => SmsDeliveryStatus.Unknown
    };
}
