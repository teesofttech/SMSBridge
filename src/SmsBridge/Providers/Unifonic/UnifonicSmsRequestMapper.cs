using SmsBridge.Abstractions;
using SmsBridge.Options;

namespace SmsBridge.Providers.Unifonic;

internal static class UnifonicSmsRequestMapper
{
    public static IReadOnlyDictionary<string, string> ToFormFields(
        SmsMessage message,
        UnifonicOptions options)
    {
        return new Dictionary<string, string>
        {
            ["AppSid"] = options.AppSid,
            ["SenderID"] = message.From ?? options.From,
            ["Body"] = message.Body,
            ["Recipient"] = message.To
        };
    }
}
