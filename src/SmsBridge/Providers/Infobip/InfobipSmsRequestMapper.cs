using SmsBridge.Abstractions;

namespace SmsBridge.Providers.Infobip;

internal static class InfobipSmsRequestMapper
{
    public static object ToRequestBody(SmsMessage message, string defaultFrom) => new
    {
        messages = new[]
        {
            new
            {
                sender = message.From ?? defaultFrom,
                destinations = new[]
                {
                    new { to = message.To.TrimStart('+') }
                },
                content = new
                {
                    text = message.Body
                }
            }
        }
    };
}
