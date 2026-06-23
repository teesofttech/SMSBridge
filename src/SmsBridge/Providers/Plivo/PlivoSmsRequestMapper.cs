using SmsBridge.Abstractions;
using SmsBridge.Options;

namespace SmsBridge.Providers.Plivo;

internal static class PlivoSmsRequestMapper
{
    public static IReadOnlyDictionary<string, object> ToRequestBody(
        SmsMessage message,
        PlivoOptions options)
    {
        var body = new Dictionary<string, object>
        {
            ["src"] = message.From ?? options.From,
            ["dst"] = message.To,
            ["text"] = message.Body
        };

        if (!string.IsNullOrWhiteSpace(options.CallbackUrl))
        {
            body["url"] = options.CallbackUrl;
            body["method"] = "POST";
        }

        return body;
    }
}
