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
    /// Registers SMSBridge and its configured providers from a configuration section.
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

        var builder = new SmsBridgeBuilder(services);
        RegisterConfiguredProviders(builder, configuration);
        return builder;
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
        services.AddSingleton<ISmsWebhookParser, SinchWebhookParser>();
        services.AddSingleton<ISmsWebhookParser, TelnyxWebhookParser>();
        services.AddSingleton<SmsWebhookParserResolver>();
    }

    private static void RegisterConfiguredProviders(
        SmsBridgeBuilder builder,
        IConfigurationSection configuration)
    {
        foreach (var providerSection in configuration.GetSection(nameof(SmsBridgeOptions.Providers)).GetChildren())
        {
            var typeValue = providerSection[nameof(SmsProviderOptions.Type)];
            if (!Enum.TryParse<SmsProviderType>(typeValue, ignoreCase: true, out var providerType) ||
                providerType == SmsProviderType.Unknown ||
                !Enum.IsDefined(providerType))
            {
                throw new SmsBridgeException(
                    $"SMS provider '{providerSection.Key}' has an unknown or missing Type '{typeValue}'.");
            }

            RegisterConfiguredProvider(builder, providerSection, providerType);
        }
    }

    private static void RegisterConfiguredProvider(
        SmsBridgeBuilder builder,
        IConfigurationSection providerSection,
        SmsProviderType providerType)
    {
        var name = providerSection.Key;

        builder.RegisterConfiguredProvider(() =>
        {
            switch (providerType)
            {
                case SmsProviderType.Twilio:
                    builder.UseTwilio(name, config => BindProviderConfiguration(providerSection, config));
                    break;
                case SmsProviderType.Vonage:
                    builder.UseVonage(name, config => BindProviderConfiguration(providerSection, config));
                    break;
                case SmsProviderType.MessageBird:
                    builder.UseMessageBird(name, config => BindProviderConfiguration(providerSection, config));
                    break;
                case SmsProviderType.AwsSns:
                    builder.UseAwsSns(name, config => BindProviderConfiguration(providerSection, config));
                    break;
                case SmsProviderType.Infobip:
                    builder.UseInfobip(name, config => BindProviderConfiguration(providerSection, config));
                    break;
                case SmsProviderType.Termii:
                    builder.UseTermii(name, config => BindProviderConfiguration(providerSection, config));
                    break;
                case SmsProviderType.Unifonic:
                    builder.UseUnifonic(name, config => BindProviderConfiguration(providerSection, config));
                    break;
                case SmsProviderType.Sinch:
                    builder.UseSinch(name, config => BindProviderConfiguration(providerSection, config));
                    break;
                case SmsProviderType.Plivo:
                    builder.UsePlivo(name, config => BindProviderConfiguration(providerSection, config));
                    break;
                case SmsProviderType.Telnyx:
                    builder.UseTelnyx(name, config => BindProviderConfiguration(providerSection, config));
                    break;
                case SmsProviderType.SmartSms:
                    builder.UseSmartSms(name, config => BindProviderConfiguration(providerSection, config));
                    break;
                case SmsProviderType.AfricasTalking:
                    builder.UseAfricasTalking(name, config => BindProviderConfiguration(providerSection, config));
                    break;
                default:
                    throw new SmsBridgeException(
                        $"SMS provider '{name}' uses unsupported Type '{providerType}'.");
            }
        });
    }

    private static void BindProviderConfiguration<TConfig>(
        IConfigurationSection providerSection,
        TConfig config)
        where TConfig : class
    {
        providerSection.Bind(config);
        providerSection.GetSection(nameof(SmsProviderOptions.Settings)).Bind(config);
    }
}
