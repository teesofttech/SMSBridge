using Microsoft.Extensions.DependencyInjection;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.Arkesel;

namespace SmsBridge.DependencyInjection;

/// <summary>Registration extensions for the Arkesel SMS V1 provider.</summary>
public static class ArkeselProviderBuilder
{
    public static SmsBridgeBuilder UseArkesel(this SmsBridgeBuilder builder, string name,
        Action<ArkeselProviderConfig> configure)
    {
        var config = new ArkeselProviderConfig();
        configure(config);

        if (string.IsNullOrWhiteSpace(config.ApiKey))
            throw new SmsBridgeException($"Arkesel provider '{name}': ApiKey is required.");
        if (string.IsNullOrWhiteSpace(config.Sender))
            throw new SmsBridgeException($"Arkesel provider '{name}': Sender is required.");

        var options = new ArkeselOptions
        {
            ApiKey = config.ApiKey,
            Sender = config.Sender,
            UseCase = config.UseCase
        };
        builder.Services.AddHttpClient(HttpClientNames.Arkesel);
        builder.AddProvider(name, sp => new ArkeselSmsProvider(
            name, options,
            sp.GetRequiredService<IHttpClientFactory>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ArkeselSmsProvider>>()));
        return builder;
    }
}

public sealed class ArkeselProviderConfig
{
    public string? ApiKey { get; set; }
    public string? Sender { get; set; }
    public string? UseCase { get; set; }
}
