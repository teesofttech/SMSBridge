using SmsBridge.Abstractions;

namespace SmsBridge.Providers.Telnyx;

internal static class TelnyxSmsRequestMapper
{
    public static object ToRequestBody(SmsMessage message, string defaultFrom) => new
    {
        from = message.From ?? defaultFrom,
        to = message.To,
        text = message.Body
    };
}
