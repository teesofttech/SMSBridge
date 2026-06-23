using System.Net;
using RichardSzalay.MockHttp;
using NSubstitute;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.Telnyx;
using Microsoft.Extensions.Logging.Abstractions;

namespace SmsBridge.ProviderTests;

public sealed class TelnyxProviderTests
{
    private static readonly TelnyxOptions Options = new()
    {
        ApiKey = "KEY01234567890_test",
        From = "+15551234567"
    };

    private static TelnyxSmsProvider BuildProvider(MockHttpMessageHandler mock)
    {
        var httpClient = mock.ToHttpClient();
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(HttpClientNames.Telnyx).Returns(httpClient);
        return new TelnyxSmsProvider("telnyx", Options, factory, NullLogger<TelnyxSmsProvider>.Instance);
    }

    [Fact]
    public async Task SendAsync_ReturnsSentResultOnSuccess()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://api.telnyx.com/*")
            .Respond("application/json", """{"data":{"id":"msg-123","to":[{"status":"queued"}]}}""");

        var provider = BuildProvider(mock);
        var result = await provider.SendAsync(new SmsMessage { To = "+447700900001", Body = "Hi" });

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("telnyx");
        result.ProviderMessageId.Should().Be("msg-123");
    }

    [Fact]
    public async Task SendAsync_ReturnsFailureOn422()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://api.telnyx.com/*")
            .Respond(HttpStatusCode.UnprocessableEntity, "application/json",
                """{"errors":[{"code":"10033","detail":"Invalid phone number"}]}""");

        var provider = BuildProvider(mock);
        var result = await provider.SendAsync(new SmsMessage { To = "invalid", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_ReturnsTransientFailureOn503()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://api.telnyx.com/*")
            .Respond(HttpStatusCode.ServiceUnavailable, "application/json", "{}");

        var provider = BuildProvider(mock);
        var result = await provider.SendAsync(new SmsMessage { To = "+1", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeTrue();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_ReturnsNonTransientFailureWhenQueuePressureNeedsIntervention()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://api.telnyx.com/*")
            .Respond(HttpStatusCode.Forbidden, "application/json",
                """{"errors":[{"code":"40318","detail":"Internal message queue is full"}]}""");

        var provider = BuildProvider(mock);
        var result = await provider.SendAsync(new SmsMessage { To = "+1", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeFalse();
        result.ErrorCode.Should().Be("40318");
    }

    [Fact]
    public async Task SendAsync_UsesFromOnMessage_WhenSupplied()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://api.telnyx.com/*")
            .Respond("application/json", """{"data":{"id":"msg-999","to":[{"status":"queued"}]}}""");

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
