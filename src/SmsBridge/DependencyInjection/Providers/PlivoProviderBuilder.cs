using Microsoft.Extensions.DependencyInjection;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.Plivo;

namespace SmsBridge.DependencyInjection;

/// <summary>Extension methods for registering the Plivo SMS provider.</summary>
public static class PlivoProviderBuilder
{
    /// <summary>Registers the Plivo SMS provider.</summary>
    public static SmsBridgeBuilder UsePlivo(
        this SmsBridgeBuilder builder,
        string name,
        Action<PlivoProviderConfig> configure)
    {
        var config = new PlivoProviderConfig();
        configure(config);

        if (string.IsNullOrWhiteSpace(config.AuthId))
            throw new SmsBridgeException($"Plivo provider '{name}': AuthId is required.");
        if (string.IsNullOrWhiteSpace(config.AuthToken))
            throw new SmsBridgeException($"Plivo provider '{name}': AuthToken is required.");
        if (string.IsNullOrWhiteSpace(config.From))
            throw new SmsBridgeException($"Plivo provider '{name}': From is required.");

        var options = new PlivoOptions
        {
            AuthId = config.AuthId,
            AuthToken = config.AuthToken,
            From = config.From,
            CallbackUrl = config.CallbackUrl
        };

        builder.Services.AddHttpClient(HttpClientNames.Plivo);

        builder.Services.AddSingleton<ISmsProvider>(sp =>
            new PlivoSmsProvider(
                name,
                options,
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<PlivoSmsProvider>>()));

        return builder;
    }
}

/// <summary>Mutable configuration object used when calling <c>.UsePlivo()</c>.</summary>
public sealed class PlivoProviderConfig
{
    public string? AuthId { get; set; }
    public string? AuthToken { get; set; }
    public string? From { get; set; }
    public string? CallbackUrl { get; set; }
}
