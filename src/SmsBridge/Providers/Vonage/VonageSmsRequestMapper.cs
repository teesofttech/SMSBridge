using SmsBridge.Abstractions;
using SmsBridge.Options;

namespace SmsBridge.Providers.Vonage;

internal static class VonageSmsRequestMapper
{
    /// <summary>Builds the JSON payload for the Vonage SMS API v1.</summary>
    public static object ToRequestBody(SmsMessage message, VonageOptions options) =>
        new
        {
            from = message.From ?? options.From,
            to = message.To,
            text = message.Body,
            api_key = options.ApiKey,
            api_secret = options.ApiSecret
        };
}
