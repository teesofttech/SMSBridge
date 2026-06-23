using SmsBridge.Abstractions;

namespace SmsBridge.Providers.MessageBird;

internal static class MessageBirdSmsRequestMapper
{
    public static IReadOnlyDictionary<string, string> ToFormFields(
        SmsMessage message,
        string defaultFrom) =>
        new Dictionary<string, string>
        {
            ["originator"] = message.From ?? defaultFrom,
            ["recipients"] = message.To.TrimStart('+'),
            ["body"] = message.Body,
            ["datacoding"] = "auto"
        };
}
