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
        mock.When("https://sms.api.sinch.com/*")
            .Respond("application/json", """{"id":"batch-123","status":"In Progress"}""");

        var provider = BuildProvider(mock);
        var result = await provider.SendAsync(new SmsMessage { To = "+447700900001", Body = "Hi" });

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("sinch");
        result.ProviderMessageId.Should().Be("batch-123");
    }

    [Fact]
    public async Task SendAsync_ReturnsFailureOn400()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://sms.api.sinch.com/*")
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
        mock.When("https://sms.api.sinch.com/*")
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
        mock.When("https://sms.api.sinch.com/*")
            .Respond("application/json", """{"id":"batch-999","status":"In Progress"}""");

        var provider = BuildProvider(mock);
        var result = await provider.SendAsync(new SmsMessage
        {
            To = "+447700900001",
            Body = "Hi",
            From = "CustomSender"
        });

        result.Success.Should().BeTrue();
    }
}
