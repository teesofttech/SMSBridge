using Microsoft.Extensions.DependencyInjection;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.Sinch;

namespace SmsBridge.DependencyInjection;

/// <summary>Extension methods for registering the Sinch SMS provider.</summary>
public static class SinchProviderBuilder
{
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
        if (!Uri.TryCreate(config.BaseUrl, UriKind.Absolute, out var sinchBaseUri) ||
            sinchBaseUri.Scheme != Uri.UriSchemeHttps)
            throw new SmsBridgeException($"Sinch provider '{name}': BaseUrl must be an absolute HTTPS URL.");

        var options = new SinchOptions
        {
            ServicePlanId = config.ServicePlanId,
            ApiToken = config.ApiToken,
            From = config.From,
            BaseUrl = config.BaseUrl.TrimEnd('/'),
            CallbackUrl = config.CallbackUrl
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
    public string BaseUrl { get; set; } = "https://us.sms.api.sinch.com";
    public string? CallbackUrl { get; set; }
}
