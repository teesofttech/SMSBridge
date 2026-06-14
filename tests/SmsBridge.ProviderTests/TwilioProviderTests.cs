using System.Net;
using RichardSzalay.MockHttp;
using NSubstitute;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.Twilio;
using Microsoft.Extensions.Logging.Abstractions;

namespace SmsBridge.ProviderTests;

public sealed class TwilioProviderTests
{
    private static readonly TwilioOptions Options = new()
    {
        AccountSid = "ACtest",
        AuthToken = "token",
        From = "+15551234567"
    };

    private static TwilioSmsProvider BuildProvider(MockHttpMessageHandler mock)
    {
        var httpClient = mock.ToHttpClient();
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(HttpClientNames.Twilio).Returns(httpClient);
        return new TwilioSmsProvider("twilio", Options, factory, NullLogger<TwilioSmsProvider>.Instance);
    }

    [Fact]
    public async Task SendAsync_ReturnsSentResultOnSuccess()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://api.twilio.com/*")
            .Respond("application/json", """{"sid":"SM123","status":"queued"}""");

        var provider = BuildProvider(mock);
        var result = await provider.SendAsync(new SmsMessage { To = "+447700900001", Body = "Hi" });

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("twilio");
        result.ProviderMessageId.Should().Be("SM123");
    }

    [Fact]
    public async Task SendAsync_ReturnsFailureResultOn400()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://api.twilio.com/*")
            .Respond(HttpStatusCode.BadRequest,
                "application/json",
                """{"code":21211,"message":"Invalid To number"}""");

        var provider = BuildProvider(mock);
        var result = await provider.SendAsync(new SmsMessage { To = "invalid", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_ReturnsTransientFailureOn503()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://api.twilio.com/*")
            .Respond(HttpStatusCode.ServiceUnavailable, "application/json", "{}");

        var provider = BuildProvider(mock);
        var result = await provider.SendAsync(new SmsMessage { To = "+1", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_UsesFromOnMessage_WhenSupplied()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://api.twilio.com/*")
            .Respond("application/json", """{"sid":"SM999","status":"queued"}""");

        var provider = BuildProvider(mock);
        var result = await provider.SendAsync(new SmsMessage
        {
            To = "+447700900001",
            Body = "Hi",
            From = "+15559999999"
        });

        result.Success.Should().BeTrue();
    }
}
