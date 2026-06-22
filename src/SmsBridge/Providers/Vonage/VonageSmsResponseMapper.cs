using System.Text.Json;
using SmsBridge.Abstractions;

namespace SmsBridge.Providers.Vonage;

internal static class VonageSmsResponseMapper
{
    public static SmsSendResult FromResponse(string providerName, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Vonage returns { "messages": [ { "status": "0", "message-id": "...", ... } ] }
        if (!root.TryGetProperty("messages", out var messages) || messages.GetArrayLength() == 0)
            return SmsSendResult.Failed(providerName, null, "Vonage returned an empty messages array");

        var first = messages[0];
        var status = first.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null;
        var messageId = first.TryGetProperty("message-id", out var idEl) ? idEl.GetString() : null;
        var errorText = first.TryGetProperty("error-text", out var errEl) ? errEl.GetString() : null;

        // Status "0" means success in Vonage API v1
        if (status == "0")
            return SmsSendResult.Succeeded(providerName, messageId, SmsDeliveryStatus.Accepted);

        bool isTransient = IsTransientStatus(status);
        return SmsSendResult.Failed(providerName, status, errorText, isTransient);
    }

    public static SmsSendResult FromErrorResponse(string providerName, int httpStatusCode, string body) =>
        SmsSendResult.Failed(
            providerName,
            httpStatusCode.ToString(),
            $"Vonage API returned HTTP {httpStatusCode}",
            isTransient: httpStatusCode >= 500 || httpStatusCode == 429);

    private static bool IsTransientStatus(string? status) => status switch
    {
        // 1 = throttled, 2 = missing params (permanent), 3 = invalid params (permanent),
        // 4 = invalid creds (permanent), 5 = internal error (transient), 6 = invalid from (permanent),
        // 10 = too many existing binds/connections (back off and retry)
        "1" or "5" or "10" => true,
        _ => false
    };
}
