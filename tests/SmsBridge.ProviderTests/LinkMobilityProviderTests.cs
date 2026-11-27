using System.Net;
using System.Text;
using System.Text.Json;
using NSubstitute;
using RichardSzalay.MockHttp;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.LinkMobility;

namespace SmsBridge.ProviderTests;

public sealed class LinkMobilityProviderTests
{
    [Theory]
    [InlineData(null, null, "https://wsx.sp247.net/sms/send", "MyApp", "ALPHANUMERIC")]
    [InlineData("https://eu.linkmobility.io/", "+4790000000", "https://eu.linkmobility.io/sms/send", "+4790000000", "MSISDN")]
    public async Task SendAsync_PostsDocumentedSingleSendWithBasicAuthentication(
        string? gateway, string? sender, string url, string expectedSource, string expectedTon)
    {
        using var handler = new MockHttpMessageHandler();
        handler.Expect(HttpMethod.Post, url)
            .WithHeaders("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:password")))
            .WithHeaders("Accept", "application/json")
            .With(request => request.Content?.Headers.ContentType?.MediaType == "application/json")
            .WithContent(JsonSerializer.Serialize(new
            {
                source = expectedSource,
                sourceTON = expectedTon,
                destination = "+4799999999",
                userData = "Hei & hallo!",
                platformId = "test-platform",
                platformPartnerId = "test-partner",
                useDeliveryReport = false,
                ignoreResponse = false
            }))
            .Respond("application/json", """{"messageId":"test-msg","resultCode":1005,"description":"Queued"}""");

        var result = await BuildProvider(handler, gateway).SendAsync(new SmsMessage
        {
            To = "+4799999999",
            From = sender,
            Body = "Hei & hallo!"
        });

        result.Success.Should().BeTrue();
        result.ProviderMessageId.Should().Be("test-msg");
        result.Status.Should().Be(SmsDeliveryStatus.Queued);
        handler.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task SendAsync_Http204_AcceptsWithoutMessageId()
    {
        using var handler = new MockHttpMessageHandler();
        handler.Expect(HttpMethod.Post, "https://wsx.sp247.net/sms/send").Respond(HttpStatusCode.NoContent);

        var result = await BuildProvider(handler, null).SendAsync(
            new SmsMessage { To = "+4799999999", Body = "Hei" });

        result.Success.Should().BeTrue();
        result.Status.Should().Be(SmsDeliveryStatus.Accepted);
        result.ProviderMessageId.Should().BeNull();
    }

    private static LinkMobilitySmsProvider BuildProvider(MockHttpMessageHandler handler, string? gateway)
    {
        var options = new LinkMobilityOptions
        {
            Username = "user",
            Password = "password",
            PlatformId = "test-platform",
            PlatformPartnerId = "test-partner",
            From = "MyApp",
            BaseUrl = gateway ?? LinkMobilityOptions.DefaultBaseUrl
        };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(HttpClientNames.LinkMobility).Returns(handler.ToHttpClient());
        return new LinkMobilitySmsProvider("link", options, factory);
    }
}
