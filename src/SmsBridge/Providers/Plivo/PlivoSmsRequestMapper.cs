using SmsBridge.Abstractions;

namespace SmsBridge.Providers.Plivo;

internal static class PlivoSmsRequestMapper
{
    public static object ToRequestBody(SmsMessage message, string defaultFrom) => new
    {
        src = message.From ?? defaultFrom,
        dst = message.To,
        text = message.Body
    };
}
