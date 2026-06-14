using SmsBridge.Abstractions;

namespace SmsBridge.Providers.Twilio;

internal static class TwilioSmsRequestMapper
{
    /// <summary>Maps a <see cref="SmsMessage"/> to Twilio REST API form fields.</summary>
    public static IEnumerable<KeyValuePair<string, string>> ToFormFields(
        SmsMessage message,
        string defaultFrom)
    {
        yield return new KeyValuePair<string, string>("To", message.To);
        yield return new KeyValuePair<string, string>("From", message.From ?? defaultFrom);
        yield return new KeyValuePair<string, string>("Body", message.Body);
    }
}
