using Microsoft.Extensions.DependencyInjection;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.AfricasTalking;

namespace SmsBridge.DependencyInjection;

/// <summary>Extension methods for registering the Africa's Talking SMS provider.</summary>
public static class AfricasTalkingProviderBuilder
{
    /// <summary>Registers the Africa's Talking SMS provider.</summary>
    public static SmsBridgeBuilder UseAfricasTalking(
        this SmsBridgeBuilder builder,
        string name,
        Action<AfricasTalkingProviderConfig> configure)
    {
        var config = new AfricasTalkingProviderConfig();
        configure(config);

        if (string.IsNullOrWhiteSpace(config.Username))
            throw new SmsBridgeException($"Africa's Talking provider '{name}': Username is required.");
        if (string.IsNullOrWhiteSpace(config.ApiKey))
            throw new SmsBridgeException($"Africa's Talking provider '{name}': ApiKey is required.");

        var options = new AfricasTalkingOptions
        {
            Username = config.Username,
            ApiKey = config.ApiKey,
            From = config.From
        };

        builder.Services.AddHttpClient(HttpClientNames.AfricasTalking);

        builder.Services.AddSingleton<ISmsProvider>(sp =>
            new AfricasTalkingSmsProvider(
                name,
                options,
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AfricasTalkingSmsProvider>>()));

        return builder;
    }
}

/// <summary>Mutable configuration object used when calling <c>.UseAfricasTalking()</c>.</summary>
public sealed class AfricasTalkingProviderConfig
{
    public string? Username { get; set; }
    public string? ApiKey { get; set; }
    public string? From { get; set; }
}
