using Microsoft.Extensions.DependencyInjection;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.SevenIo;

namespace SmsBridge.DependencyInjection;

public static class SevenIoProviderBuilder
{
    public static SmsBridgeBuilder UseSevenIo(this SmsBridgeBuilder builder, string name,
        Action<SevenIoProviderConfig> configure)
    {
        var config = new SevenIoProviderConfig();
        configure(config);

        if (string.IsNullOrWhiteSpace(config.ApiKey))
            throw new SmsBridgeException($"seven.io provider '{name}' requires an API key.");
        if (string.IsNullOrWhiteSpace(config.From))
            throw new SmsBridgeException($"seven.io provider '{name}' requires a sender.");

        var options = new SevenIoOptions { ApiKey = config.ApiKey, From = config.From };
        builder.Services.AddHttpClient(HttpClientNames.SevenIo)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        builder.AddProvider(name, services => new SevenIoSmsProvider(
            name, options, services.GetRequiredService<IHttpClientFactory>()));
        return builder;
    }
}

public sealed class SevenIoProviderConfig
{
    public string? ApiKey { get; set; }
    public string? From { get; set; }
}
