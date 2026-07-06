using Microsoft.Extensions.DependencyInjection;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.AwsSns;
using SmsBridge.Providers.Infobip;
using SmsBridge.Providers.MessageBird;
using SmsBridge.Providers.Plivo;
using SmsBridge.Providers.Telnyx;
using SmsBridge.Providers.Termii;
using SmsBridge.Providers.Sinch;
using SmsBridge.Providers.Twilio;
using SmsBridge.Providers.Vonage;

namespace SmsBridge.DependencyInjection;

/// <summary>
/// Extension methods on <see cref="SmsBridgeBuilder"/> for registering providers.
/// </summary>
public static class SmsBridgeProviderBuilder
{
    /// <summary>Registers the AWS SNS SMS provider.</summary>
    public static SmsBridgeBuilder UseAwsSns(
        this SmsBridgeBuilder builder,
        string name,
        Action<AwsSnsProviderConfig> configure)
    {
        var config = new AwsSnsProviderConfig();
        configure(config);

        if (string.IsNullOrWhiteSpace(config.AccessKeyId))
            throw new SmsBridgeException($"AWS SNS provider '{name}': AccessKeyId is required.");
        if (string.IsNullOrWhiteSpace(config.SecretAccessKey))
            throw new SmsBridgeException($"AWS SNS provider '{name}': SecretAccessKey is required.");
        if (string.IsNullOrWhiteSpace(config.Region))
            throw new SmsBridgeException($"AWS SNS provider '{name}': Region is required.");

        var options = new AwsSnsOptions
        {
            AccessKeyId = config.AccessKeyId,
            SecretAccessKey = config.SecretAccessKey,
            Region = config.Region,
            SenderId = config.SenderId
        };

        builder.Services.AddHttpClient(HttpClientNames.AwsSns);

        builder.Services.AddSingleton<ISmsProvider>(sp =>
            new AwsSnsSmsProvider(
                name,
                options,
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AwsSnsSmsProvider>>()));

        return builder;
    }

    /// <summary>Registers the Termii SMS provider.</summary>
    public static SmsBridgeBuilder UseTermii(
        this SmsBridgeBuilder builder,
        string name,
        Action<TermiiProviderConfig> configure)
    {
        var config = new TermiiProviderConfig();
        configure(config);

        if (string.IsNullOrWhiteSpace(config.ApiKey))
            throw new SmsBridgeException($"Termii provider '{name}': ApiKey is required.");
        if (string.IsNullOrWhiteSpace(config.From))
            throw new SmsBridgeException($"Termii provider '{name}': From is required.");
        if (string.IsNullOrWhiteSpace(config.Channel))
            throw new SmsBridgeException($"Termii provider '{name}': Channel is required.");
        if (!Uri.TryCreate(config.BaseUrl, UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme != Uri.UriSchemeHttps)
            throw new SmsBridgeException($"Termii provider '{name}': BaseUrl must be an absolute HTTPS URL.");

        var options = new TermiiOptions
        {
            ApiKey = config.ApiKey,
            From = config.From,
            Channel = config.Channel,
            BaseUrl = config.BaseUrl.TrimEnd('/')
        };

        builder.Services.AddHttpClient(HttpClientNames.Termii);

        builder.Services.AddSingleton<ISmsProvider>(sp =>
            new TermiiSmsProvider(
                name,
                options,
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<TermiiSmsProvider>>()));

        return builder;
    }

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
            From = config.From,
            StatusCallbackUrl = config.StatusCallbackUrl
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

    /// <summary>Registers the Telnyx SMS provider.</summary>
    public static SmsBridgeBuilder UseTelnyx(
        this SmsBridgeBuilder builder,
        string name,
        Action<TelnyxProviderConfig> configure)
    {
        var config = new TelnyxProviderConfig();
        configure(config);

        if (string.IsNullOrWhiteSpace(config.ApiKey))
            throw new SmsBridgeException($"Telnyx provider '{name}': ApiKey is required.");
        if (string.IsNullOrWhiteSpace(config.From))
            throw new SmsBridgeException($"Telnyx provider '{name}': From is required.");

        var options = new TelnyxOptions
        {
            ApiKey = config.ApiKey,
            From = config.From
        };

        builder.Services.AddHttpClient(HttpClientNames.Telnyx);

        builder.Services.AddSingleton<ISmsProvider>(sp =>
            new TelnyxSmsProvider(
                name,
                options,
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<TelnyxSmsProvider>>()));

        return builder;
    }

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

/// <summary>Mutable configuration object used when calling <c>.UseAwsSns()</c>.</summary>
public sealed class AwsSnsProviderConfig
{
    public string? AccessKeyId { get; set; }
    public string? SecretAccessKey { get; set; }
    public string? Region { get; set; }
    public string? SenderId { get; set; }
}

/// <summary>Mutable configuration object used when calling <c>.UseTermii()</c>.</summary>
public sealed class TermiiProviderConfig
{
    public string? ApiKey { get; set; }
    public string? From { get; set; }
    public string Channel { get; set; } = "generic";
    public string BaseUrl { get; set; } = "https://api.ng.termii.com";
}

/// <summary>Mutable configuration object used when calling <c>.UseInfobip()</c>.</summary>
public sealed class InfobipProviderConfig
{
    public string? ApiKey { get; set; }
    public string? BaseUrl { get; set; }
    public string? From { get; set; }
}

/// <summary>Mutable configuration object used when calling <c>.UseMessageBird()</c>.</summary>
public sealed class MessageBirdProviderConfig
{
    public string? AccessKey { get; set; }
    public string? From { get; set; }
}

/// <summary>Mutable configuration object used when calling <c>.UseTelnyx()</c>.</summary>
public sealed class TelnyxProviderConfig
{
    public string? ApiKey { get; set; }
    public string? From { get; set; }
}

/// <summary>Mutable configuration object used when calling <c>.UsePlivo()</c>.</summary>
public sealed class PlivoProviderConfig
{
    public string? AuthId { get; set; }
    public string? AuthToken { get; set; }
    public string? From { get; set; }
    public string? CallbackUrl { get; set; }
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

/// <summary>Mutable configuration object used when calling <c>.UseTwilio()</c>.</summary>
public sealed class TwilioProviderConfig
{
    public string? AccountSid { get; set; }
    public string? AuthToken { get; set; }
    public string? From { get; set; }
    public string? StatusCallbackUrl { get; set; }
}

/// <summary>Mutable configuration object used when calling <c>.UseVonage()</c>.</summary>
public sealed class VonageProviderConfig
{
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? From { get; set; }
}
