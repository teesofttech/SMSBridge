using System.Globalization;
using System.Text.Json;
using SmsBridge.Abstractions;
using SmsBridge.Internal;

namespace SmsBridge.Providers.Exotel;

internal static class ExotelSmsResponseMapper
{
    internal static SmsSendResult FromResponse(string providerName, string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("SMSMessage", out var message) && message.ValueKind == JsonValueKind.Object)
            {
                var sid = ReadString(message, "Sid");
                var status = ReadString(message, "Status") switch
                {
                    "queued" => SmsDeliveryStatus.Queued,
                    "sending" => SmsDeliveryStatus.Accepted,
                    "submitted" => SmsDeliveryStatus.Sent,
                    "sent" => SmsDeliveryStatus.Delivered,
                    "failed" or "failed-dnd" or "failed_dnd" => SmsDeliveryStatus.Failed,
                    _ => SmsDeliveryStatus.Unknown
                };
                if (!string.IsNullOrWhiteSpace(sid) && status != SmsDeliveryStatus.Unknown)
                {
                    return status == SmsDeliveryStatus.Failed
                        ? new SmsSendResult
                        {
                            Provider = providerName,
                            ProviderMessageId = sid,
                            Status = status,
                            ErrorMessage = "Exotel reported an SMS delivery failure."
                        }
                        : SmsSendResult.Succeeded(providerName, sid, status);
                }
            }
        }
        catch (JsonException)
        {
            // An unreadable acknowledgement can still follow an accepted send.
        }

        return ProviderResponseFailures.InvalidSuccessResponse(providerName, "Exotel");
    }

    internal static SmsSendResult FromHttpError(string providerName, int status)
    {
        var ambiguous = status == 408 || status >= 500;
        return SmsSendResult.Failed(providerName, status.ToString(CultureInfo.InvariantCulture),
            $"Exotel returned HTTP {status}.", isTransient: ambiguous || status == 429,
            mayHaveBeenAccepted: ambiguous);
    }

    private static string? ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
