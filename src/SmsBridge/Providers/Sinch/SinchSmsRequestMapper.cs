using SmsBridge.Abstractions;

namespace SmsBridge.Providers.Sinch;

internal static class SinchSmsRequestMapper
{
    public static object ToRequestBody(SmsMessage message, string defaultFrom) => new
    {
        from = message.From ?? defaultFrom,
        to = new[] { message.To },
        body = message.Body
    };
}
