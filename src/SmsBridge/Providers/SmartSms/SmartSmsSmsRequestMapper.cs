using SmsBridge.Abstractions;
using SmsBridge.Options;

namespace SmsBridge.Providers.SmartSms;

internal static class SmartSmsSmsRequestMapper
{
    public static IReadOnlyDictionary<string, string> ToRequestBody(
        SmsMessage message,
        SmartSmsOptions options)
    {
        var body = new Dictionary<string, string>
        {
            ["token"] = options.Token,
            ["sender"] = message.From ?? options.From,
            ["to"] = message.To,
            ["message"] = message.Body,
            ["type"] = ReadMetadata(message, "type") ?? "0",
            ["routing"] = ReadMetadata(message, "routing") ?? "3",
            ["ref_id"] = ReadMetadata(message, "ref_id") ?? ReadMetadata(message, "refId") ?? string.Empty
        };

        AddOptionalMetadata(body, message, "simserver_token");
        AddOptionalMetadata(body, message, "dlr_timeout");
        AddOptionalMetadata(body, message, "schedule");

        return body;
    }

    private static void AddOptionalMetadata(
        IDictionary<string, string> body,
        SmsMessage message,
        string key)
    {
        var value = ReadMetadata(message, key);
        if (value is not null)
            body[key] = value;
    }

    private static string? ReadMetadata(SmsMessage message, string key)
    {
        if (message.Metadata is null)
            return null;

        return message.Metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }
}
