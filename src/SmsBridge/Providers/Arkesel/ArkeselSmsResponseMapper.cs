using System.Text.Json;
using SmsBridge.Abstractions;
using SmsBridge.Internal;

namespace SmsBridge.Providers.Arkesel;

internal static class ArkeselSmsResponseMapper
{
    internal static SmsSendResult FromResponse(string providerName, string body)
    {
        var raw = body.Trim();
        if (string.Equals(raw, "OK", StringComparison.OrdinalIgnoreCase))
            return SmsSendResult.Succeeded(providerName, null);
        if (IsV1Error(raw))
            return SmsSendResult.Failed(providerName, raw, Describe(raw));

        try
        {
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return ProviderResponseFailures.InvalidSuccessResponse(providerName, "Arkesel");

            var code = Read(root, "code") ?? Read(root, "status");
            var description = Read(root, "message") ?? Read(root, "description") ?? Read(root, "error");
            var reference = Read(root, "message_id") ?? Read(root, "messageId") ?? Read(root, "id");

            if (string.Equals(code, "OK", StringComparison.OrdinalIgnoreCase) ||
                (string.IsNullOrWhiteSpace(code) && string.Equals(description, "OK", StringComparison.OrdinalIgnoreCase)))
                return SmsSendResult.Succeeded(providerName, reference);

            if (!string.IsNullOrWhiteSpace(code))
                return SmsSendResult.Failed(providerName, code, description ?? Describe(code));

            return ProviderResponseFailures.InvalidSuccessResponse(providerName, "Arkesel");
        }
        catch (JsonException)
        {
            return ProviderResponseFailures.InvalidSuccessResponse(providerName, "Arkesel");
        }
    }

    internal static SmsSendResult FromErrorResponse(string providerName, int status, string body)
    {
        var code = status.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string? description = null;
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                code = Read(document.RootElement, "code") ?? Read(document.RootElement, "status") ?? code;
                description = Read(document.RootElement, "message") ?? Read(document.RootElement, "error");
            }
        }
        catch (JsonException)
        {
            if (IsV1Error(body.Trim()))
                code = body.Trim();
        }

        var transient = status == 429 || status >= 500;
        return SmsSendResult.Failed(providerName, code, description ?? Describe(code),
            isTransient: transient, mayHaveBeenAccepted: status >= 500);
    }

    private static bool IsV1Error(string code) =>
        code is "100" or "101" or "102" or "103" or "104" or "105" or "106" or "109" or "111";

    private static string Describe(string code) =>
        IsV1Error(code) ? $"Arkesel SMS API returned error code {code}." : $"Arkesel SMS API returned status {code}.";

    private static string? Read(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property))
            return null;
        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            _ => null
        };
    }
}
