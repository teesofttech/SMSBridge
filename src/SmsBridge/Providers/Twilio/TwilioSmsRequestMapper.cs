using SmsBridge.Abstractions;
using SmsBridge.Options;

namespace SmsBridge.Providers.Twilio;

internal static class TwilioSmsRequestMapper
{
    /// <summary>Maps a <see cref="SmsMessage"/> to Twilio REST API form fields.</summary>
    public static IEnumerable<KeyValuePair<string, string>> ToFormFields(
        SmsMessage message,
        TwilioOptions options)
    {
        yield return new KeyValuePair<string, string>("To", message.To);
        yield return new KeyValuePair<string, string>("From", message.From ?? options.From);
        yield return new KeyValuePair<string, string>("Body", message.Body);

        if (!string.IsNullOrWhiteSpace(options.StatusCallbackUrl))
            yield return new KeyValuePair<string, string>("StatusCallback", options.StatusCallbackUrl);
    }
}
