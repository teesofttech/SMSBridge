using SmsBridge.Abstractions;
using SmsBridge.Options;

namespace SmsBridge.Providers.Vonage;

internal static class VonageSmsRequestMapper
{
    /// <summary>Builds the form fields for the Vonage SMS API v1.</summary>
    public static IReadOnlyDictionary<string, string> ToFormFields(SmsMessage message, VonageOptions options) =>
        new Dictionary<string, string>
        {
            ["from"] = message.From ?? options.From,
            ["to"] = message.To,
            ["text"] = message.Body,
            ["api_key"] = options.ApiKey,
            ["api_secret"] = options.ApiSecret
        };
}
