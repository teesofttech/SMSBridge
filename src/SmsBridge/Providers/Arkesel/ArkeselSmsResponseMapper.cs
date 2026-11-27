using System.Globalization;
using System.Text.Json;
using SmsBridge.Abstractions;
using SmsBridge.Internal;

namespace SmsBridge.Providers.Arkesel;

internal static class ArkeselSmsResponseMapper
{
    internal static SmsSendResult FromResponse(string providerName, string recipient, string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var status = ReadString(root, "status");

            if (status == "error")
                return SmsSendResult.Failed(providerName, "error", "Arkesel rejected the SMS request.");

            if (status == "success")
            {
                if (root.TryGetProperty("data", out var data))
                    return FromRecipientResults(providerName, recipient, data);

                if (!string.IsNullOrWhiteSpace(ReadString(root, "message")))
                    return SmsSendResult.Succeeded(providerName, null);
            }
        }
        catch (JsonException)
        {
            // A malformed success response cannot rule out the SMS being accepted.
        }

        return ProviderResponseFailures.InvalidSuccessResponse(providerName, "Arkesel");
    }

    internal static SmsSendResult FromErrorResponse(string providerName, int status)
    {
        var description = status switch
        {
            401 => "Arkesel did not authorise the SMS request.",
            402 => "Arkesel reported insufficient balance or invalid coverage.",
            403 => "Arkesel refused the SMS request.",
            422 => "Arkesel could not validate the SMS request.",
            429 => "Arkesel rate-limited the SMS request.",
            _ => $"Arkesel returned HTTP {status}."
        };

        return SmsSendResult.Failed(providerName, status.ToString(CultureInfo.InvariantCulture), description,
            isTransient: status == 429 || status >= 500, mayHaveBeenAccepted: status >= 500);
    }

    private static SmsSendResult FromRecipientResults(string providerName, string recipient, JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Array)
            return ProviderResponseFailures.InvalidSuccessResponse(providerName, "Arkesel");

        var number = recipient.TrimStart('+');
        foreach (var entry in data.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
                continue;

            if (ReadString(entry, "recipient")?.TrimStart('+') == number)
                return SmsSendResult.Succeeded(providerName, ReadString(entry, "id"));

            if (entry.TryGetProperty("invalid numbers", out var invalidNumbers) &&
                invalidNumbers.ValueKind == JsonValueKind.Array &&
                invalidNumbers.EnumerateArray().Any(value =>
                    value.ValueKind == JsonValueKind.String && value.GetString()?.TrimStart('+') == number))
            {
                return SmsSendResult.Failed(providerName, "invalid_recipient", "Arkesel rejected the recipient number.");
            }
        }

        return ProviderResponseFailures.InvalidSuccessResponse(providerName, "Arkesel");
    }

    private static string? ReadString(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(name, out var property) &&
        property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
}
