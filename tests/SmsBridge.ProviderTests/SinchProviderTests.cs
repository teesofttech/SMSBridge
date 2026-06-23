using System.Net;
using RichardSzalay.MockHttp;
using NSubstitute;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.Sinch;
using Microsoft.Extensions.Logging.Abstractions;

namespace SmsBridge.ProviderTests;

public sealed class SinchProviderTests
{
    private static readonly SinchOptions Options = new()
    {
        ServicePlanId = "plan-123",
        ApiToken = "test-token",
        From = "SmsBridge"
    };

    private static SinchSmsProvider BuildProvider(MockHttpMessageHandler mock)
    {
        var httpClient = mock.ToHttpClient();
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(HttpClientNames.Sinch).Returns(httpClient);
        return new SinchSmsProvider("sinch", Options, factory, NullLogger<SinchSmsProvider>.Instance);
    }

    [Fact]
    public async Task SendAsync_ReturnsSentResultOnSuccess()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://us.sms.api.sinch.com/*")
            .Respond("application/json", """{"id":"batch-123","to":["+447700900001"],"from":"SmsBridge","body":"Hi"}""");

        var provider = BuildProvider(mock);
        var result = await provider.SendAsync(new SmsMessage { To = "+447700900001", Body = "Hi" });

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("sinch");
        result.ProviderMessageId.Should().Be("batch-123");
        result.Status.Should().Be(SmsDeliveryStatus.Queued);
    }

    [Fact]
    public async Task SendAsync_ReturnsFailureOn400()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://us.sms.api.sinch.com/*")
            .Respond(HttpStatusCode.BadRequest, "application/json", """{"code":"400","text":"Invalid parameter"}""");

        var provider = BuildProvider(mock);
        var result = await provider.SendAsync(new SmsMessage { To = "invalid", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_ReturnsTransientFailureOn503()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://us.sms.api.sinch.com/*")
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
        mock.When("https://us.sms.api.sinch.com/*")
            .Respond("application/json", """{"id":"batch-999","to":["+447700900001"],"from":"CustomSender","body":"Hi"}""");

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
    public void RequestMapper_AddsConfiguredPerRecipientDeliveryReport()
    {
        var options = new SinchOptions
        {
            ServicePlanId = "plan-123",
            ApiToken = "test-token",
            From = "SmsBridge",
            CallbackUrl = "https://example.com/webhooks/sinch"
        };

        var body = SinchSmsRequestMapper.ToRequestBody(
            new SmsMessage { To = "+447700900001", Body = "Hi" },
            options);

        body["delivery_report"].Should().Be("per_recipient");
        body["callback_url"].Should().Be("https://example.com/webhooks/sinch");
    }
}
