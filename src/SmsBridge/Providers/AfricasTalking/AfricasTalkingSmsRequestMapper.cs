using SmsBridge.Abstractions;
using SmsBridge.Options;

namespace SmsBridge.Providers.AfricasTalking;

internal static class AfricasTalkingSmsRequestMapper
{
    public static IReadOnlyDictionary<string, string> ToRequestBody(
        SmsMessage message,
        AfricasTalkingOptions options)
    {
        var body = new Dictionary<string, string>
        {
            ["username"] = options.Username,
            ["to"] = message.To,
            ["message"] = message.Body
        };

        var from = message.From ?? options.From;
        if (!string.IsNullOrWhiteSpace(from))
            body["from"] = from;

        return body;
    }
}
