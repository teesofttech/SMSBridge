using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmsBridge.Abstractions;
using SmsBridge.DependencyInjection;
using SmsBridge.Options;

namespace SmsBridge.IntegrationTests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void ISmsClient_CanBeResolvedFromDI()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSmsBridge(opts =>
            {
                opts.DefaultProvider = "twilio";
                opts.Providers["twilio"] = new SmsProviderOptions { Type = SmsProviderType.Twilio };
            })
            .UseTwilio("twilio", o =>
            {
                o.AccountSid = "ACtest";
                o.AuthToken = "token";
                o.From = "+15551234567";
            });

        var provider = services.BuildServiceProvider();

        var client = provider.GetService<ISmsClient>();
        client.Should().NotBeNull();
    }

    [Fact]
    public void ISmsClient_CanBeResolvedFromDI_WithTelnyx()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSmsBridge(opts =>
            {
                opts.DefaultProvider = "telnyx";
                opts.Providers["telnyx"] = new SmsProviderOptions { Type = SmsProviderType.Telnyx };
            })
            .UseTelnyx("telnyx", o =>
            {
                o.ApiKey = "KEY01234567890_test";
                o.From = "+15551234567";
            });

        var provider = services.BuildServiceProvider();

        var client = provider.GetService<ISmsClient>();
        client.Should().NotBeNull();
    }

    [Fact]
    public void ISmsClient_CanBeResolvedFromDI_WithPlivo()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSmsBridge(opts =>
            {
                opts.DefaultProvider = "plivo";
                opts.Providers["plivo"] = new SmsProviderOptions { Type = SmsProviderType.Plivo };
            })
            .UsePlivo("plivo", o =>
            {
                o.AuthId = "MATEST000000000000000";
                o.AuthToken = "test-token";
                o.From = "SmsBridge";
            });

        var provider = services.BuildServiceProvider();

        var client = provider.GetService<ISmsClient>();
        client.Should().NotBeNull();
    }

    [Fact]
    public void ISmsClient_CanBeResolvedFromDI_WithSinch()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSmsBridge(opts =>
            {
                opts.DefaultProvider = "sinch";
                opts.Providers["sinch"] = new SmsProviderOptions { Type = SmsProviderType.Sinch };
            })
            .UseSinch("sinch", o =>
            {
                o.ServicePlanId = "plan-123";
                o.ApiToken = "test-token";
                o.From = "SmsBridge";
            });

        var provider = services.BuildServiceProvider();

        var client = provider.GetService<ISmsClient>();
        client.Should().NotBeNull();
    }

    [Fact]
    public void ISmsClient_CanBeResolvedFromDI_WithMessageBird()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSmsBridge(opts =>
            {
                opts.DefaultProvider = "messagebird";
                opts.Providers["messagebird"] = new SmsProviderOptions
                {
                    Type = SmsProviderType.MessageBird
                };
            })
            .UseMessageBird("messagebird", o =>
            {
                o.AccessKey = "test-access-key";
                o.From = "SmsBridge";
            });

        var provider = services.BuildServiceProvider();

        var client = provider.GetService<ISmsClient>();
        client.Should().NotBeNull();
    }

    [Fact]
    public void BothTwilioAndVonage_CanBeRegisteredTogether()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSmsBridge(opts =>
            {
                opts.DefaultProvider = "twilio";
                opts.EnableFailover = true;
                opts.FailoverProvider = "vonage";
                opts.Providers["twilio"] = new SmsProviderOptions { Type = SmsProviderType.Twilio };
                opts.Providers["vonage"] = new SmsProviderOptions { Type = SmsProviderType.Vonage };
            })
            .UseTwilio("twilio", o =>
            {
                o.AccountSid = "ACtest";
                o.AuthToken = "token";
                o.From = "+15551234567";
            })
            .UseVonage("vonage", o =>
            {
                o.ApiKey = "key";
                o.ApiSecret = "secret";
                o.From = "MyApp";
            });

        var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ISmsClient>();
        client.Should().NotBeNull();
    }
}
