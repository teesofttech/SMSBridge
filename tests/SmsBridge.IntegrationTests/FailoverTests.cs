using System.Net;
using RichardSzalay.MockHttp;
using Microsoft.Extensions.DependencyInjection;
using SmsBridge.Abstractions;
using SmsBridge.DependencyInjection;
using SmsBridge.Internal.Http;
using SmsBridge.Options;

namespace SmsBridge.IntegrationTests;

public sealed class FailoverTests
{
    private static IServiceCollection BaseServices() =>
        new ServiceCollection().AddLogging();

    private static SmsBridgeBuilder ConfigureBothProviders(
        IServiceCollection services,
        Action<SmsBridgeOptions>? extra = null) =>
        services.AddSmsBridge(opts =>
            {
                opts.DefaultProvider = "twilio";
                opts.EnableFailover = true;
                opts.FailoverProvider = "vonage";
                opts.Providers["twilio"] = new SmsProviderOptions { Type = SmsProviderType.Twilio };
                opts.Providers["vonage"] = new SmsProviderOptions { Type = SmsProviderType.Vonage };
                extra?.Invoke(opts);
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

    [Fact]
    public async Task SmsClient_DoesNotFailOver_WhenPrimaryMayHaveAcceptedMessage()
    {
        var twilioMock = new MockHttpMessageHandler();
        twilioMock.When("https://api.twilio.com/*")
            .Respond(HttpStatusCode.ServiceUnavailable, "application/json", "{}");

        var vonageMock = new MockHttpMessageHandler();
        var vonageRequest = vonageMock.When("https://rest.nexmo.com/sms/json")
            .Respond("application/json", """{"messages":[{"status":"0","message-id":"V999"}]}""");

        var services = BaseServices();
        services.AddHttpClient(HttpClientNames.Twilio)
            .ConfigurePrimaryHttpMessageHandler(() => twilioMock);
        services.AddHttpClient(HttpClientNames.Vonage)
            .ConfigurePrimaryHttpMessageHandler(() => vonageMock);

        ConfigureBothProviders(services);

        var sp = services.BuildServiceProvider();
        var client = sp.GetRequiredService<ISmsClient>();

        var result = await client.SendAsync(new SmsMessage { To = "+447700900001", Body = "Test" });

        result.Success.Should().BeFalse();
        result.Provider.Should().Be("twilio");
        result.IsTransientFailure.Should().BeTrue();
        result.MayHaveBeenAccepted.Should().BeTrue();
        vonageMock.GetMatchCount(vonageRequest).Should().Be(0);
    }

    [Fact]
    public async Task SmsClient_FailsOverToVonage_WhenTwilioIsRateLimited()
    {
        var twilioMock = new MockHttpMessageHandler();
        twilioMock.When("https://api.twilio.com/*")
            .Respond(HttpStatusCode.TooManyRequests, "application/json", "{}");

        var vonageMock = new MockHttpMessageHandler();
        vonageMock.When("https://rest.nexmo.com/sms/json")
            .Respond("application/json", """{"messages":[{"status":"0","message-id":"V999"}]}""");

        var services = BaseServices();
        services.AddHttpClient(HttpClientNames.Twilio)
            .ConfigurePrimaryHttpMessageHandler(() => twilioMock);
        services.AddHttpClient(HttpClientNames.Vonage)
            .ConfigurePrimaryHttpMessageHandler(() => vonageMock);

        ConfigureBothProviders(services);

        var sp = services.BuildServiceProvider();
        var client = sp.GetRequiredService<ISmsClient>();

        var result = await client.SendAsync(new SmsMessage { To = "+447700900001", Body = "Test" });

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("vonage");
    }

    [Fact]
    public async Task SmsClient_DoesNotFailOver_WhenErrorIsNotTransient()
    {
        var twilioMock = new MockHttpMessageHandler();
        twilioMock.When("https://api.twilio.com/*")
            .Respond(HttpStatusCode.BadRequest,
                "application/json",
                """{"code":21211,"message":"Invalid number"}""");

        var services = BaseServices();
        services.AddHttpClient(HttpClientNames.Twilio)
            .ConfigurePrimaryHttpMessageHandler(() => twilioMock);

        ConfigureBothProviders(services);

        var sp = services.BuildServiceProvider();
        var client = sp.GetRequiredService<ISmsClient>();

        var result = await client.SendAsync(new SmsMessage { To = "invalid", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.Provider.Should().Be("twilio");
    }

    [Fact]
    public async Task SmsClient_DoesNotFailOver_WhenExplicitProviderRequested()
    {
        var twilioMock = new MockHttpMessageHandler();
        twilioMock.When("https://api.twilio.com/*")
            .Respond(HttpStatusCode.ServiceUnavailable, "application/json", "{}");

        var services = BaseServices();
        services.AddHttpClient(HttpClientNames.Twilio)
            .ConfigurePrimaryHttpMessageHandler(() => twilioMock);

        // Change default to vonage so explicit "twilio" overrides it
        services.AddSmsBridge(opts =>
            {
                opts.DefaultProvider = "vonage";
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

        var sp = services.BuildServiceProvider();
        var client = sp.GetRequiredService<ISmsClient>();

        // Explicitly request twilio — transient failure should NOT cause failover
        var result = await client.SendAsync(new SmsMessage
        {
            To = "+447700900001",
            Body = "Hi",
            Provider = "twilio"
        });

        result.Success.Should().BeFalse();
        result.Provider.Should().Be("twilio");
    }
}
