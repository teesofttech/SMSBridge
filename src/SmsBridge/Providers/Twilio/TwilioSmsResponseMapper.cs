using System.Text.Json;
using SmsBridge.Abstractions;

namespace SmsBridge.Providers.Twilio;

internal static class TwilioSmsResponseMapper
{
    public static SmsSendResult FromResponse(string providerName, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Twilio returns a "status" field and optionally "error_code" / "error_message".
        var status = root.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null;
        var sid = root.TryGetProperty("sid", out var sidEl) ? sidEl.GetString() : null;
        var errorCode = root.TryGetProperty("error_code", out var ecEl) ? ReadErrorCode(ecEl) : null;
        var errorMessage = root.TryGetProperty("error_message", out var emEl) ? emEl.GetString() : null;

        var deliveryStatus = MapDeliveryStatus(status);

        if (deliveryStatus == SmsDeliveryStatus.Failed || deliveryStatus == SmsDeliveryStatus.Undelivered)
        {
            return SmsSendResult.Failed(providerName, errorCode, errorMessage,
                isTransient: IsTransientError(errorCode), status: deliveryStatus);
        }

        return SmsSendResult.Succeeded(providerName, sid, deliveryStatus);
    }

    public static SmsSendResult FromErrorResponse(string providerName, int httpStatusCode, string json)
    {
        string? errorCode = null;
        string? errorMessage = null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            errorCode = root.TryGetProperty("code", out var codeEl) ? codeEl.GetInt32().ToString() : null;
            errorMessage = root.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : null;
        }
        catch (JsonException) { /* best-effort */ }

        bool isTransient = httpStatusCode >= 500 || httpStatusCode == 429;
        return SmsSendResult.Failed(providerName, errorCode, errorMessage ?? $"HTTP {httpStatusCode}", isTransient);
    }

    private static SmsDeliveryStatus MapDeliveryStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "accepted" => SmsDeliveryStatus.Accepted,
        "queued" => SmsDeliveryStatus.Queued,
        "sending" or "sent" => SmsDeliveryStatus.Sent,
        "delivered" => SmsDeliveryStatus.Delivered,
        "undelivered" => SmsDeliveryStatus.Undelivered,
        "failed" => SmsDeliveryStatus.Failed,
        _ => SmsDeliveryStatus.Unknown
    };

    private static bool IsTransientError(string? code) => code switch
    {
        // 30001 is queue overflow and Twilio explicitly recommends retrying later.
        // 30003 can represent a temporarily unreachable handset or carrier path.
        // Other delivery errors require remediation or are too ambiguous to retry safely.
        "30001" or "30003" => true,
        _ => false
    };

    private static string? ReadErrorCode(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Number => element.GetRawText(),
        JsonValueKind.String => element.GetString(),
        _ => null
    };
}
