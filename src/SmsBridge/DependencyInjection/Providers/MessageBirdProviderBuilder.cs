using Microsoft.Extensions.DependencyInjection;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.MessageBird;

namespace SmsBridge.DependencyInjection;

/// <summary>Extension methods for registering the MessageBird SMS provider.</summary>
public static class MessageBirdProviderBuilder
{
    /// <summary>Registers the MessageBird SMS provider.</summary>
    public static SmsBridgeBuilder UseMessageBird(
        this SmsBridgeBuilder builder,
        string name,
        Action<MessageBirdProviderConfig> configure)
    {
        var config = new MessageBirdProviderConfig();
        configure(config);

        if (string.IsNullOrWhiteSpace(config.AccessKey))
            throw new SmsBridgeException($"MessageBird provider '{name}': AccessKey is required.");
        if (string.IsNullOrWhiteSpace(config.From))
            throw new SmsBridgeException($"MessageBird provider '{name}': From is required.");

        var options = new MessageBirdOptions
        {
            AccessKey = config.AccessKey,
            From = config.From
        };

        builder.Services.AddHttpClient(HttpClientNames.MessageBird);

        builder.Services.AddSingleton<ISmsProvider>(sp =>
            new MessageBirdSmsProvider(
                name,
                options,
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MessageBirdSmsProvider>>()));

        return builder;
    }
}

/// <summary>Mutable configuration object used when calling <c>.UseMessageBird()</c>.</summary>
public sealed class MessageBirdProviderConfig
{
    public string? AccessKey { get; set; }
    public string? From { get; set; }
}
