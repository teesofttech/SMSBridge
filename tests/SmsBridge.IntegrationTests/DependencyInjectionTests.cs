using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmsBridge.Abstractions;
using SmsBridge.DependencyInjection;
using SmsBridge.Options;
using SmsBridge.Routing;

namespace SmsBridge.IntegrationTests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void ConfigurationSection_RegistersProviderFromSettingsSubsection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SmsBridge:DefaultProvider"] = "twilio",
                ["SmsBridge:Providers:twilio:Type"] = "Twilio",
                ["SmsBridge:Providers:twilio:Settings:AccountSid"] = "ACtest",
                ["SmsBridge:Providers:twilio:Settings:AuthToken"] = "token",
                ["SmsBridge:Providers:twilio:Settings:From"] = "+15551234567"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSmsBridge(configuration.GetSection("SmsBridge"));

        var provider = services.BuildServiceProvider().GetServices<ISmsProvider>().Single();
        provider.Name.Should().Be("twilio");
        provider.Type.Should().Be(SmsProviderType.Twilio);
    }

    [Theory]
    [InlineData(SmsProviderType.Twilio, "AccountSid=ACtest|AuthToken=token|From=+15551234567")]
    [InlineData(SmsProviderType.Vonage, "ApiKey=key|ApiSecret=secret|From=SmsBridge")]
    [InlineData(SmsProviderType.MessageBird, "AccessKey=key|From=SmsBridge")]
    [InlineData(SmsProviderType.AwsSns, "AccessKeyId=key|SecretAccessKey=secret|Region=eu-west-2")]
    [InlineData(SmsProviderType.Infobip, "ApiKey=key|BaseUrl=https://example.api.infobip.com|From=SmsBridge")]
    [InlineData(SmsProviderType.Termii, "ApiKey=key|From=SmsBridge")]
    [InlineData(SmsProviderType.Unifonic, "AppSid=app-sid|From=SmsBridge")]
    [InlineData(SmsProviderType.Sinch, "ServicePlanId=plan-id|ApiToken=token|From=SmsBridge")]
    [InlineData(SmsProviderType.Plivo, "AuthId=auth-id|AuthToken=token|From=SmsBridge")]
    [InlineData(SmsProviderType.Telnyx, "ApiKey=key|From=+15551234567")]
    [InlineData(SmsProviderType.SmartSms, "Token=token|From=SmsBridge")]
    [InlineData(SmsProviderType.AfricasTalking, "Username=user|ApiKey=key")]
    public void ConfigurationSection_RegistersConfiguredProvider(
        SmsProviderType providerType,
        string serialisedSettings)
    {
        const string providerName = "primary";
        var settings = new Dictionary<string, string?>
        {
            ["SmsBridge:DefaultProvider"] = providerName,
            [$"SmsBridge:Providers:{providerName}:Type"] = providerType.ToString()
        };
        foreach (var setting in serialisedSettings.Split('|'))
        {
            var pair = setting.Split('=', 2);
            settings[$"SmsBridge:Providers:{providerName}:{pair[0]}"] = pair[1];
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSmsBridge(configuration.GetSection("SmsBridge"));

        var serviceProvider = services.BuildServiceProvider();
        var providers = serviceProvider.GetServices<ISmsProvider>().ToArray();

        providers.Should().ContainSingle();
        providers[0].Name.Should().Be(providerName);
        providers[0].Type.Should().Be(providerType);

        var router = serviceProvider.GetRequiredService<FailoverSmsProviderRouter>();
        var resolvedProvider = router.Resolve(new SmsRoutingContext
        {
            Message = new SmsMessage { To = "+15551234567", Body = "Test" }
        });
        resolvedProvider.Should().BeSameAs(providers[0]);
    }

    [Fact]
    public void ConfigurationSection_WithFluentProviderOfSameName_UsesFluentRegistration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SmsBridge:DefaultProvider"] = "primary",
                ["SmsBridge:Providers:primary:Type"] = "Twilio",
                ["SmsBridge:Providers:primary:AccountSid"] = "ACtest",
                ["SmsBridge:Providers:primary:AuthToken"] = "token",
                ["SmsBridge:Providers:primary:From"] = "+15551234567"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSmsBridge(configuration.GetSection("SmsBridge"))
            .UseVonage("PRIMARY", options =>
            {
                options.ApiKey = "key";
                options.ApiSecret = "secret";
                options.From = "SmsBridge";
            });

        var serviceProvider = services.BuildServiceProvider();
        var providers = serviceProvider.GetServices<ISmsProvider>().ToArray();

        providers.Should().ContainSingle();
        providers[0].Name.Should().Be("PRIMARY");
        providers[0].Type.Should().Be(SmsProviderType.Vonage);

        var router = serviceProvider.GetRequiredService<FailoverSmsProviderRouter>();
        router.Resolve(new SmsRoutingContext
        {
            Message = new SmsMessage { To = "+15551234567", Body = "Test" }
        }).Should().BeSameAs(providers[0]);
    }

    [Fact]
    public void ConfigurationSection_WithEquivalentFluentProvider_RegistersOneProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SmsBridge:DefaultProvider"] = "twilio",
                ["SmsBridge:Providers:twilio:Type"] = "Twilio",
                ["SmsBridge:Providers:twilio:AccountSid"] = "ACconfigured",
                ["SmsBridge:Providers:twilio:AuthToken"] = "configured-token",
                ["SmsBridge:Providers:twilio:From"] = "+15551234567"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSmsBridge(configuration.GetSection("SmsBridge"))
            .UseTwilio("twilio", options =>
            {
                options.AccountSid = "ACexplicit";
                options.AuthToken = "explicit-token";
                options.From = "+15557654321";
            });

        using var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetServices<ISmsProvider>().Should().ContainSingle();
        serviceProvider.GetRequiredService<ISmsClient>().Should().NotBeNull();
    }

    [Fact]
    public void FluentProvider_WithDuplicateName_ThrowsDescriptiveSmsBridgeException()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddSmsBridge(options =>
        {
            options.DefaultProvider = "primary";
            options.Providers["primary"] = new SmsProviderOptions { Type = SmsProviderType.Twilio };
        });
        builder.UseTwilio("primary", options =>
        {
            options.AccountSid = "ACtest";
            options.AuthToken = "token";
            options.From = "+15551234567";
        });

        var act = () => builder.UseVonage("PRIMARY", options =>
        {
            options.ApiKey = "key";
            options.ApiSecret = "secret";
            options.From = "SmsBridge";
        });

        act.Should().Throw<SmsBridgeException>()
            .WithMessage("*already been registered explicitly*case-insensitive*");
    }

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
    public void ISmsClient_CanBeResolvedFromDI_WithAwsSns()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSmsBridge(opts =>
            {
                opts.DefaultProvider = "aws";
                opts.Providers["aws"] = new SmsProviderOptions { Type = SmsProviderType.AwsSns };
            })
            .UseAwsSns("aws", o =>
            {
                o.AccessKeyId = "AKIDEXAMPLE";
                o.SecretAccessKey = "secret";
                o.Region = "eu-west-2";
                o.SenderId = "SmsBridge";
            });

        var provider = services.BuildServiceProvider();

        var client = provider.GetService<ISmsClient>();
        client.Should().NotBeNull();
    }

    [Fact]
    public void ISmsClient_CanBeResolvedFromDI_WithUnifonic()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSmsBridge(opts =>
            {
                opts.DefaultProvider = "unifonic";
                opts.Providers["unifonic"] = new SmsProviderOptions { Type = SmsProviderType.Unifonic };
            })
            .UseUnifonic("unifonic", o =>
            {
                o.AppSid = "test-app-sid";
                o.From = "SmsBridge";
            });

        var provider = services.BuildServiceProvider();

        var client = provider.GetService<ISmsClient>();
        client.Should().NotBeNull();
    }

    [Fact]
    public void ISmsClient_CanBeResolvedFromDI_WithAfricasTalking()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSmsBridge(opts =>
            {
                opts.DefaultProvider = "africas-talking";
                opts.Providers["africas-talking"] = new SmsProviderOptions { Type = SmsProviderType.AfricasTalking };
            })
            .UseAfricasTalking("africas-talking", o =>
            {
                o.Username = "test-username";
                o.ApiKey = "test-api-key";
                o.From = "SmsBridge";
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
    public void ISmsClient_CanBeResolvedFromDI_WithInfobip()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSmsBridge(opts =>
            {
                opts.DefaultProvider = "infobip";
                opts.Providers["infobip"] = new SmsProviderOptions
                {
                    Type = SmsProviderType.Infobip
                };
            })
            .UseInfobip("infobip", o =>
            {
                o.ApiKey = "test-api-key";
                o.BaseUrl = "https://example.api.infobip.com";
                o.From = "SmsBridge";
            });

        var provider = services.BuildServiceProvider();

        var client = provider.GetService<ISmsClient>();
        client.Should().NotBeNull();
    }

    [Fact]
    public void ISmsClient_CanBeResolvedFromDI_WithSmartSms()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSmsBridge(opts =>
            {
                opts.DefaultProvider = "smart-sms";
                opts.Providers["smart-sms"] = new SmsProviderOptions
                {
                    Type = SmsProviderType.SmartSms
                };
            })
            .UseSmartSms("smart-sms", o =>
            {
                o.Token = "test-token";
                o.From = "SmsBridge";
            });

        var provider = services.BuildServiceProvider();

        var client = provider.GetService<ISmsClient>();
        client.Should().NotBeNull();
    }

    [Fact]
    public void ISmsClient_CanBeResolvedFromDI_WithTermii()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSmsBridge(opts =>
            {
                opts.DefaultProvider = "termii";
                opts.Providers["termii"] = new SmsProviderOptions
                {
                    Type = SmsProviderType.Termii
                };
            })
            .UseTermii("termii", o =>
            {
                o.ApiKey = "test-api-key";
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
