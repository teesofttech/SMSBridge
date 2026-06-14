using Microsoft.Extensions.DependencyInjection;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.Sinch;
using SmsBridge.Providers.Twilio;
using SmsBridge.Providers.Vonage;

namespace SmsBridge.DependencyInjection;

/// <summary>
/// Extension methods on <see cref="SmsBridgeBuilder"/> for registering providers.
/// </summary>
public static class SmsBridgeProviderBuilder
{
    /// <summary>Registers the Twilio SMS provider.</summary>
    public static SmsBridgeBuilder UseTwilio(
        this SmsBridgeBuilder builder,
        string name,
        Action<TwilioProviderConfig> configure)
    {
        var config = new TwilioProviderConfig();
        configure(config);

        if (string.IsNullOrWhiteSpace(config.AccountSid))
            throw new SmsBridgeException($"Twilio provider '{name}': AccountSid is required.");
        if (string.IsNullOrWhiteSpace(config.AuthToken))
            throw new SmsBridgeException($"Twilio provider '{name}': AuthToken is required.");
        if (string.IsNullOrWhiteSpace(config.From))
            throw new SmsBridgeException($"Twilio provider '{name}': From is required.");

        var options = new TwilioOptions
        {
            AccountSid = config.AccountSid,
            AuthToken = config.AuthToken,
            From = config.From
        };

        builder.Services.AddHttpClient(HttpClientNames.Twilio);

        builder.Services.AddSingleton<ISmsProvider>(sp =>
            new TwilioSmsProvider(
                name,
                options,
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<TwilioSmsProvider>>()));

        return builder;
    }

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

    /// <summary>Registers the Sinch SMS provider.</summary>
    public static SmsBridgeBuilder UseSinch(
        this SmsBridgeBuilder builder,
        string name,
        Action<SinchProviderConfig> configure)
    {
        var config = new SinchProviderConfig();
        configure(config);

        if (string.IsNullOrWhiteSpace(config.ServicePlanId))
            throw new SmsBridgeException($"Sinch provider '{name}': ServicePlanId is required.");
        if (string.IsNullOrWhiteSpace(config.ApiToken))
            throw new SmsBridgeException($"Sinch provider '{name}': ApiToken is required.");
        if (string.IsNullOrWhiteSpace(config.From))
            throw new SmsBridgeException($"Sinch provider '{name}': From is required.");

        var options = new SinchOptions
        {
            ServicePlanId = config.ServicePlanId,
            ApiToken = config.ApiToken,
            From = config.From
        };

        builder.Services.AddHttpClient(HttpClientNames.Sinch);

        builder.Services.AddSingleton<ISmsProvider>(sp =>
            new SinchSmsProvider(
                name,
                options,
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SinchSmsProvider>>()));

        return builder;
    }
}

/// <summary>Mutable configuration object used when calling <c>.UseSinch()</c>.</summary>
public sealed class SinchProviderConfig
{
    public string? ServicePlanId { get; set; }
    public string? ApiToken { get; set; }
    public string? From { get; set; }
}

/// <summary>Mutable configuration object used when calling <c>.UseTwilio()</c>.</summary>
public sealed class TwilioProviderConfig
{
    public string? AccountSid { get; set; }
    public string? AuthToken { get; set; }
    public string? From { get; set; }
}

/// <summary>Mutable configuration object used when calling <c>.UseVonage()</c>.</summary>
public sealed class VonageProviderConfig
{
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? From { get; set; }
}
