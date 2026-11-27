using System.Globalization;
using System.Text.Json;
using SmsBridge.Abstractions;
using SmsBridge.Internal;

namespace SmsBridge.Providers.LinkMobility;

internal static class LinkMobilitySmsResponseMapper
{
    internal static SmsSendResult FromResponse(string providerName, string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (ReadResultCode(root) == 1005 &&
                root.TryGetProperty("messageId", out var id) &&
                id.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(id.GetString()))
            {
                return SmsSendResult.Succeeded(providerName, id.GetString(), SmsDeliveryStatus.Queued);
            }
        }
        catch (JsonException)
        {
            // A missing acknowledgement can still follow an accepted send.
        }

        return ProviderResponseFailures.InvalidSuccessResponse(providerName, "LINK Mobility");
    }

    internal static SmsSendResult FromHttpError(string providerName, int status, string body)
    {
        int? resultCode = null;
        try
        {
            using var document = JsonDocument.Parse(body);
            resultCode = ReadResultCode(document.RootElement);
        }
        catch (JsonException)
        {
            // Keep the HTTP status when no structured error is available.
        }

        var ambiguous = status == 408 || status >= 500;
        return SmsSendResult.Failed(providerName, (resultCode ?? status).ToString(CultureInfo.InvariantCulture),
            $"LINK Mobility returned HTTP {status}.", isTransient: ambiguous || status == 429,
            mayHaveBeenAccepted: ambiguous);
    }

    private static int? ReadResultCode(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("resultCode", out var code))
            return null;

        if (code.ValueKind == JsonValueKind.Number && code.TryGetInt32(out var number))
            return number;
        if (code.ValueKind == JsonValueKind.String &&
            int.TryParse(code.GetString(), NumberStyles.None, CultureInfo.InvariantCulture, out number))
        {
            return number;
        }

        return null;
    }
}
