using Microsoft.Extensions.DependencyInjection;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.Telnyx;

namespace SmsBridge.DependencyInjection;

/// <summary>Extension methods for registering the Telnyx SMS provider.</summary>
public static class TelnyxProviderBuilder
{
    /// <summary>Registers the Telnyx SMS provider.</summary>
    public static SmsBridgeBuilder UseTelnyx(
        this SmsBridgeBuilder builder,
        string name,
        Action<TelnyxProviderConfig> configure)
    {
        var config = new TelnyxProviderConfig();
        configure(config);

        if (string.IsNullOrWhiteSpace(config.ApiKey))
            throw new SmsBridgeException($"Telnyx provider '{name}': ApiKey is required.");
        if (string.IsNullOrWhiteSpace(config.From))
            throw new SmsBridgeException($"Telnyx provider '{name}': From is required.");

        var options = new TelnyxOptions
        {
            ApiKey = config.ApiKey,
            From = config.From
        };

        builder.Services.AddHttpClient(HttpClientNames.Telnyx);

        builder.Services.AddSingleton<ISmsProvider>(sp =>
            new TelnyxSmsProvider(
                name,
                options,
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<TelnyxSmsProvider>>()));

        return builder;
    }
}

/// <summary>Mutable configuration object used when calling <c>.UseTelnyx()</c>.</summary>
public sealed class TelnyxProviderConfig
{
    public string? ApiKey { get; set; }
    public string? From { get; set; }
}
