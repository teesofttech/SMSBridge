using System.Globalization;
using System.Text.Json;
using SmsBridge.Abstractions;
using SmsBridge.Internal;

namespace SmsBridge.Providers.Textmagic;

internal static class TextmagicSmsResponseMapper
{
    internal static SmsSendResult FromResponse(string providerName, string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String)
            {
                switch (type.GetString())
                {
                    case "message":
                        var messageId = ReadPositiveNumber(root, "messageId") ?? ReadPositiveNumber(root, "id");
                        if (messageId is not null)
                            return SmsSendResult.Succeeded(providerName, messageId);
                        break;
                    case "session":
                    case "bulk":
                        if (ReadPositiveNumber(root, "id") is not null)
                            return SmsSendResult.Succeeded(providerName, null);
                        break;
                }
            }
        }
        catch (JsonException)
        {
            // An unreadable success response can still represent an accepted SMS.
        }

        return ProviderResponseFailures.InvalidSuccessResponse(providerName, "Textmagic");
    }

    internal static SmsSendResult FromHttpError(string providerName, int status, string body)
    {
        var code = status.ToString(CultureInfo.InvariantCulture);
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
                code = ReadPositiveNumber(document.RootElement, "code") ?? code;
        }
        catch (JsonException)
        {
            // Keep the HTTP status when the provider didn't return a JSON error.
        }

        var ambiguous = status == 408 || status >= 500;
        return SmsSendResult.Failed(providerName, code, $"Textmagic returned HTTP {status}.",
            isTransient: ambiguous || status == 429, mayHaveBeenAccepted: ambiguous);
    }

    private static string? ReadPositiveNumber(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number) && number > 0
            ? number.ToString(CultureInfo.InvariantCulture)
            : null;
}
