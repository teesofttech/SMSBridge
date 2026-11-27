using System.Net;
using NSubstitute;
using RichardSzalay.MockHttp;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.SevenIo;

namespace SmsBridge.ProviderTests;

public sealed class SevenIoProviderTests
{
    [Theory]
    [InlineData(null, "SmsBridge")]
    [InlineData("OtherApp", "OtherApp")]
    public async Task SendAsync_PostsFormWithApiKeyHeader(string? messageSender, string expectedSender)
    {
        using var handler = new MockHttpMessageHandler();
        handler.Expect(HttpMethod.Post, "https://gateway.seven.io/api/sms")
            .With(request => request.RequestUri?.Query == string.Empty &&
                request.Content?.Headers.ContentType?.MediaType == "application/x-www-form-urlencoded")
            .WithHeaders("X-Api-Key", "test-key")
            .WithHeaders("Accept", "text/plain")
            .WithContent($"to=%2B4917612345678&from={expectedSender}&text=Hallo+%26+hello%3F")
            .Respond("text/plain", "100\r\n");

        var result = await BuildProvider(handler).SendAsync(new SmsMessage
        {
            To = "+4917612345678",
            From = messageSender,
            Body = "Hallo & hello?"
        });

        result.Success.Should().BeTrue();
        result.Status.Should().Be(SmsDeliveryStatus.Accepted);
        handler.VerifyNoOutstandingExpectation();
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, "600", "600", true, true)]
    [InlineData(HttpStatusCode.OK, "900", "900", false, false)]
    [InlineData(HttpStatusCode.TooManyRequests, "", "429", true, false)]
    [InlineData(HttpStatusCode.ServiceUnavailable, "", "503", true, true)]
    public async Task SendAsync_MapsGatewayAndHttpFailures(
        HttpStatusCode status, string body, string expectedCode, bool transient, bool ambiguous)
    {
        using var handler = new MockHttpMessageHandler();
        handler.Expect(HttpMethod.Post, "https://gateway.seven.io/api/sms")
            .Respond(status, "text/plain", body);

        var result = await BuildProvider(handler).SendAsync(new SmsMessage
        {
            To = "+4917612345678",
            Body = "Hallo"
        });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(expectedCode);
        result.IsTransientFailure.Should().Be(transient);
        result.MayHaveBeenAccepted.Should().Be(ambiguous);
    }

    private static SevenIoSmsProvider BuildProvider(MockHttpMessageHandler handler)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(HttpClientNames.SevenIo).Returns(handler.ToHttpClient());
        return new SevenIoSmsProvider("seven",
            new SevenIoOptions { ApiKey = "test-key", From = "SmsBridge" }, factory);
    }
}
