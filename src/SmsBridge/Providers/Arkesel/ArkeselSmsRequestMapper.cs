using SmsBridge.Abstractions;
using SmsBridge.Options;

namespace SmsBridge.Providers.Arkesel;

internal static class ArkeselSmsRequestMapper
{
    internal static Uri ToRequestUri(SmsMessage message, ArkeselOptions options)
    {
        var values = new List<KeyValuePair<string, string>>
        {
            new("action", "send-sms"),
            new("api_key", options.ApiKey),
            new("to", message.To),
            new("from", message.From ?? options.Sender),
            new("sms", message.Body)
        };
        if (!string.IsNullOrWhiteSpace(options.UseCase))
            values.Add(new("use_case", options.UseCase));
        return new Uri("https://sms.arkesel.com/sms/api?" +
            string.Join("&", values.Select(value =>
                Uri.EscapeDataString(value.Key) + "=" + Uri.EscapeDataString(value.Value))));
    }
}
