using Microsoft.Extensions.DependencyInjection;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.Textmagic;

namespace SmsBridge.DependencyInjection;

public static class TextmagicProviderBuilder
{
    public static SmsBridgeBuilder UseTextmagic(this SmsBridgeBuilder builder, string name,
        Action<TextmagicProviderConfig> configure)
    {
        var config = new TextmagicProviderConfig();
        configure(config);

        if (string.IsNullOrWhiteSpace(config.Username))
            throw new SmsBridgeException($"Textmagic provider '{name}' requires a username.");
        if (string.IsNullOrWhiteSpace(config.ApiKey))
            throw new SmsBridgeException($"Textmagic provider '{name}' requires an API key.");
        if (string.IsNullOrWhiteSpace(config.From))
            throw new SmsBridgeException($"Textmagic provider '{name}' requires a sender.");

        var options = new TextmagicOptions { Username = config.Username, ApiKey = config.ApiKey, From = config.From };
        builder.Services.AddHttpClient(HttpClientNames.Textmagic)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        builder.AddProvider(name, services => new TextmagicSmsProvider(
            name, options, services.GetRequiredService<IHttpClientFactory>()));
        return builder;
    }
}

public sealed class TextmagicProviderConfig
{
    public string? Username { get; set; }
    public string? ApiKey { get; set; }
    public string? From { get; set; }
}
