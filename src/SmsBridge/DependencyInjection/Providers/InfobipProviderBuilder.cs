using Microsoft.Extensions.DependencyInjection;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.Infobip;

namespace SmsBridge.DependencyInjection;

/// <summary>Extension methods for registering the Infobip SMS provider.</summary>
public static class InfobipProviderBuilder
{
    /// <summary>Registers the Infobip SMS provider.</summary>
    public static SmsBridgeBuilder UseInfobip(
        this SmsBridgeBuilder builder,
        string name,
        Action<InfobipProviderConfig> configure)
    {
        var config = new InfobipProviderConfig();
        configure(config);

        if (string.IsNullOrWhiteSpace(config.ApiKey))
            throw new SmsBridgeException($"Infobip provider '{name}': ApiKey is required.");
        if (string.IsNullOrWhiteSpace(config.BaseUrl))
            throw new SmsBridgeException($"Infobip provider '{name}': BaseUrl is required.");
        if (!Uri.TryCreate(config.BaseUrl, UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme != Uri.UriSchemeHttps)
            throw new SmsBridgeException($"Infobip provider '{name}': BaseUrl must be an absolute HTTPS URL.");
        if (string.IsNullOrWhiteSpace(config.From))
            throw new SmsBridgeException($"Infobip provider '{name}': From is required.");

        var options = new InfobipOptions
        {
            ApiKey = config.ApiKey,
            BaseUrl = config.BaseUrl.TrimEnd('/'),
            From = config.From
        };

        builder.Services.AddHttpClient(HttpClientNames.Infobip);

        builder.Services.AddSingleton<ISmsProvider>(sp =>
            new InfobipSmsProvider(
                name,
                options,
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<InfobipSmsProvider>>()));

        return builder;
    }
}

/// <summary>Mutable configuration object used when calling <c>.UseInfobip()</c>.</summary>
public sealed class InfobipProviderConfig
{
    public string? ApiKey { get; set; }
    public string? BaseUrl { get; set; }
    public string? From { get; set; }
}
