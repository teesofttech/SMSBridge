using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SmsBridge.Abstractions;
using SmsBridge.Options;
using SmsBridge.Routing;
using SmsBridge.Validation;
using SmsBridge.Webhooks;

namespace SmsBridge.DependencyInjection;

public static class SmsBridgeServiceCollectionExtensions
{
    /// <summary>
    /// Registers SMSBridge from a configuration section.
    /// </summary>
    /// <example>
    /// builder.Services.AddSmsBridge(builder.Configuration.GetSection("SmsBridge"));
    /// </example>
    public static SmsBridgeBuilder AddSmsBridge(
        this IServiceCollection services,
        IConfigurationSection configuration)
    {
        services.Configure<SmsBridgeOptions>(configuration);
        RegisterCore(services);
        return new SmsBridgeBuilder(services);
    }

    /// <summary>
    /// Registers SMSBridge with inline code configuration.
    /// </summary>
    public static SmsBridgeBuilder AddSmsBridge(
        this IServiceCollection services,
        Action<SmsBridgeOptions> configure)
    {
        services.Configure(configure);
        RegisterCore(services);
        return new SmsBridgeBuilder(services);
    }

    private static void RegisterCore(IServiceCollection services)
    {
        services.AddSingleton<IValidateOptions<SmsBridgeOptions>, SmsBridgeOptionsValidator>();

        services.AddSingleton<FailoverSmsProviderRouter>();
        services.AddSingleton<ISmsProviderRouter>(sp => sp.GetRequiredService<FailoverSmsProviderRouter>());

        services.AddSingleton<ISmsClient, SmsClient>();

        // Webhook parsers
        services.AddSingleton<ISmsWebhookParser, TwilioWebhookParser>();
        services.AddSingleton<ISmsWebhookParser, VonageWebhookParser>();
        services.AddSingleton<ISmsWebhookParser, PlivoWebhookParser>();
        services.AddSingleton<SmsWebhookParserResolver>();
    }
}
