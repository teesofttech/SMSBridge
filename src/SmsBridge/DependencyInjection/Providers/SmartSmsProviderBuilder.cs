using Microsoft.Extensions.DependencyInjection;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.SmartSms;

namespace SmsBridge.DependencyInjection;

/// <summary>Extension methods for registering the SmartSMSSolutions SMS provider.</summary>
public static class SmartSmsProviderBuilder
{
    /// <summary>Registers the SmartSMSSolutions SMS provider.</summary>
    public static SmsBridgeBuilder UseSmartSms(
        this SmsBridgeBuilder builder,
        string name,
        Action<SmartSmsProviderConfig> configure)
    {
        var config = new SmartSmsProviderConfig();
        configure(config);

        if (string.IsNullOrWhiteSpace(config.Token))
            throw new SmsBridgeException($"SmartSMSSolutions provider '{name}': Token is required.");
        if (string.IsNullOrWhiteSpace(config.From))
            throw new SmsBridgeException($"SmartSMSSolutions provider '{name}': From is required.");

        var options = new SmartSmsOptions
        {
            Token = config.Token,
            From = config.From
        };

        builder.Services.AddHttpClient(HttpClientNames.SmartSms);

        builder.Services.AddSingleton<ISmsProvider>(sp =>
            new SmartSmsSmsProvider(
                name,
                options,
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SmartSmsSmsProvider>>()));

        return builder;
    }
}

/// <summary>Mutable configuration object used when calling <c>.UseSmartSms()</c>.</summary>
public sealed class SmartSmsProviderConfig
{
    public string? Token { get; set; }
    public string? From { get; set; }
}
