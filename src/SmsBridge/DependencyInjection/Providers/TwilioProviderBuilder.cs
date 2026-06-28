using Microsoft.Extensions.DependencyInjection;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.Twilio;

namespace SmsBridge.DependencyInjection;

/// <summary>Extension methods for registering the Twilio SMS provider.</summary>
public static class TwilioProviderBuilder
{
    /// <summary>Registers the Twilio SMS provider.</summary>
    public static SmsBridgeBuilder UseTwilio(
        this SmsBridgeBuilder builder,
        string name,
        Action<TwilioProviderConfig> configure)
    {
        var config = new TwilioProviderConfig();
        configure(config);

        if (string.IsNullOrWhiteSpace(config.AccountSid))
            throw new SmsBridgeException($"Twilio provider '{name}': AccountSid is required.");
        if (string.IsNullOrWhiteSpace(config.AuthToken))
            throw new SmsBridgeException($"Twilio provider '{name}': AuthToken is required.");
        if (string.IsNullOrWhiteSpace(config.From))
            throw new SmsBridgeException($"Twilio provider '{name}': From is required.");

        var options = new TwilioOptions
        {
            AccountSid = config.AccountSid,
            AuthToken = config.AuthToken,
            From = config.From,
            StatusCallbackUrl = config.StatusCallbackUrl
        };

        builder.Services.AddHttpClient(HttpClientNames.Twilio);

        builder.Services.AddSingleton<ISmsProvider>(sp =>
            new TwilioSmsProvider(
                name,
                options,
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<TwilioSmsProvider>>()));

        return builder;
    }
}

/// <summary>Mutable configuration object used when calling <c>.UseTwilio()</c>.</summary>
public sealed class TwilioProviderConfig
{
    public string? AccountSid { get; set; }
    public string? AuthToken { get; set; }
    public string? From { get; set; }
    public string? StatusCallbackUrl { get; set; }
}
