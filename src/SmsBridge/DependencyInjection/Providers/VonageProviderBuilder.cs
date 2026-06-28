using Microsoft.Extensions.DependencyInjection;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.Vonage;

namespace SmsBridge.DependencyInjection;

/// <summary>Extension methods for registering the Vonage SMS provider.</summary>
public static class VonageProviderBuilder
{
    /// <summary>Registers the Vonage SMS provider.</summary>
    public static SmsBridgeBuilder UseVonage(
        this SmsBridgeBuilder builder,
        string name,
        Action<VonageProviderConfig> configure)
    {
        var config = new VonageProviderConfig();
        configure(config);

        if (string.IsNullOrWhiteSpace(config.ApiKey))
            throw new SmsBridgeException($"Vonage provider '{name}': ApiKey is required.");
        if (string.IsNullOrWhiteSpace(config.ApiSecret))
            throw new SmsBridgeException($"Vonage provider '{name}': ApiSecret is required.");
        if (string.IsNullOrWhiteSpace(config.From))
            throw new SmsBridgeException($"Vonage provider '{name}': From is required.");

        var options = new VonageOptions
        {
            ApiKey = config.ApiKey,
            ApiSecret = config.ApiSecret,
            From = config.From
        };

        builder.Services.AddHttpClient(HttpClientNames.Vonage);

        builder.Services.AddSingleton<ISmsProvider>(sp =>
            new VonageSmsProvider(
                name,
                options,
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<VonageSmsProvider>>()));

        return builder;
    }
}

/// <summary>Mutable configuration object used when calling <c>.UseVonage()</c>.</summary>
public sealed class VonageProviderConfig
{
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? From { get; set; }
}
