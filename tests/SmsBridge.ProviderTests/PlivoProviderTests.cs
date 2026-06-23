using System.Net;
using RichardSzalay.MockHttp;
using NSubstitute;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.Plivo;
using Microsoft.Extensions.Logging.Abstractions;

namespace SmsBridge.ProviderTests;

public sealed class PlivoProviderTests
{
    private static readonly PlivoOptions Options = new()
    {
        AuthId = "MATEST000000000000000",
        AuthToken = "test-auth-token",
        From = "SmsBridge"
    };

    private static PlivoSmsProvider BuildProvider(MockHttpMessageHandler mock)
    {
        var httpClient = mock.ToHttpClient();
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(HttpClientNames.Plivo).Returns(httpClient);
        return new PlivoSmsProvider("plivo", Options, factory, NullLogger<PlivoSmsProvider>.Instance);
    }

    [Fact]
    public async Task SendAsync_ReturnsSentResultOnSuccess()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://api.plivo.com/*")
            .Respond("application/json", """{"message_uuid":["msg-123"],"message":"message(s) queued"}""");

        var provider = BuildProvider(mock);
        var result = await provider.SendAsync(new SmsMessage { To = "+447700900001", Body = "Hi" });

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("plivo");
        result.ProviderMessageId.Should().Be("msg-123");
    }

    [Fact]
    public async Task SendAsync_ReturnsFailureOn400()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://api.plivo.com/*")
            .Respond(HttpStatusCode.BadRequest, "application/json", """{"error":"Invalid destination number"}""");

        var provider = BuildProvider(mock);
        var result = await provider.SendAsync(new SmsMessage { To = "invalid", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_ReturnsTransientFailureOn503()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://api.plivo.com/*")
            .Respond(HttpStatusCode.ServiceUnavailable, "application/json", "{}");

        var provider = BuildProvider(mock);
        var result = await provider.SendAsync(new SmsMessage { To = "+1", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeTrue();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_UsesFromOnMessage_WhenSupplied()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://api.plivo.com/*")
            .Respond("application/json", """{"message_uuid":["msg-999"],"message":"message(s) queued"}""");

        var provider = BuildProvider(mock);
        var result = await provider.SendAsync(new SmsMessage
        {
            To = "+447700900001",
            Body = "Hi",
            From = "CustomSender"
        });

        result.Success.Should().BeTrue();
    }

    [Fact]
    public void RequestMapper_AddsConfiguredDeliveryCallback()
    {
        var options = new PlivoOptions
        {
            AuthId = "MATEST000000000000000",
            AuthToken = "test-auth-token",
            From = "SmsBridge",
            CallbackUrl = "https://example.com/webhooks/plivo"
        };

        var body = PlivoSmsRequestMapper.ToRequestBody(
            new SmsMessage { To = "+447700900001", Body = "Hi" },
            options);

        body["url"].Should().Be("https://example.com/webhooks/plivo");
        body["method"].Should().Be("POST");
    }
}
