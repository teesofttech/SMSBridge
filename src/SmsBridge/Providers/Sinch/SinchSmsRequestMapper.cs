using SmsBridge.Abstractions;
using SmsBridge.Options;

namespace SmsBridge.Providers.Sinch;

internal static class SinchSmsRequestMapper
{
    public static IReadOnlyDictionary<string, object> ToRequestBody(
        SmsMessage message,
        SinchOptions options)
    {
        var body = new Dictionary<string, object>
        {
            ["from"] = message.From ?? options.From,
            ["to"] = new[] { message.To },
            ["body"] = message.Body
        };

        if (!string.IsNullOrWhiteSpace(options.CallbackUrl))
        {
            body["delivery_report"] = "per_recipient";
            body["callback_url"] = options.CallbackUrl;
        }

        return body;
    }
}
