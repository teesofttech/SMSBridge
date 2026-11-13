using System.Text.Json;
using SmsBridge.Abstractions;
using SmsBridge.Internal;

namespace SmsBridge.Providers.Telnyx;

internal static class TelnyxSmsResponseMapper
{
    public static SmsSendResult FromResponse(string providerName, string json)
    {
        try
        {
            return ParseResponse(providerName, json);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            return ProviderResponseFailures.InvalidSuccessResponse(providerName, "Telnyx");
        }
    }

    private static SmsSendResult ParseResponse(string providerName, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
            return ProviderResponseFailures.InvalidSuccessResponse(providerName, "Telnyx");

        var id = data.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(id))
            return ProviderResponseFailures.InvalidSuccessResponse(providerName, "Telnyx");

        if (!data.TryGetProperty("to", out var toEl) ||
            toEl.ValueKind != JsonValueKind.Array ||
            toEl.GetArrayLength() == 0 ||
            toEl[0].ValueKind != JsonValueKind.Object ||
            !toEl[0].TryGetProperty("status", out var statusEl))
        {
            return ProviderResponseFailures.InvalidSuccessResponse(providerName, "Telnyx");
        }

        var status = statusEl.GetString();
        if (string.IsNullOrWhiteSpace(status))
            return ProviderResponseFailures.InvalidSuccessResponse(providerName, "Telnyx");

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
        return SmsSendResult.Failed(
            providerName,
            errorCode,
            errorMessage ?? $"HTTP {httpStatusCode}",
            isTransient,
            mayHaveBeenAccepted: httpStatusCode >= 500);
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
