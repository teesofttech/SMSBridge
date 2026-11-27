using System.Net;
using System.Text;
using NSubstitute;
using RichardSzalay.MockHttp;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.Exotel;

namespace SmsBridge.ProviderTests;

public sealed class ExotelProviderTests
{
    [Theory]
    [InlineData(ExotelRegion.Singapore, "api.exotel.com", null, "EXOTEL", false)]
    [InlineData(ExotelRegion.Mumbai, "api.in.exotel.com", "MYAPPS", "MYAPPS", true)]
    public async Task SendAsync_PostsFormWithApiCredentialsAndRegionalEndpoint(
        ExotelRegion region, string host, string? sender, string expectedSender, bool includeDlt)
    {
        using var handler = new MockHttpMessageHandler();
        var expectedForm = $"From={expectedSender}&To=%2B919876543210&Body=Hello+%26+welcome%3F";
        if (includeDlt)
            expectedForm += "&DltEntityId=entity-id&DltTemplateId=template-id";

        handler.Expect(HttpMethod.Post, $"https://{host}/v1/Accounts/test-account/Sms/send.json")
            .WithHeaders("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("test-key:test-token")))
            .WithHeaders("Accept", "application/json")
            .With(request => request.RequestUri!.UserInfo == "" && request.RequestUri.Query == "" &&
                request.Content?.Headers.ContentType?.MediaType == "application/x-www-form-urlencoded")
            .WithContent(expectedForm)
            .Respond("application/json", """{"SMSMessage":{"Sid":"sms-123","Status":"queued"}}""");

        var result = await BuildProvider(handler, region, includeDlt).SendAsync(new SmsMessage
        {
            To = "+919876543210",
            From = sender,
            Body = "Hello & welcome?"
        });

        result.Success.Should().BeTrue();
        result.ProviderMessageId.Should().Be("sms-123");
        result.Status.Should().Be(SmsDeliveryStatus.Queued);
        handler.VerifyNoOutstandingExpectation();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "400", false, false)]
    [InlineData(HttpStatusCode.Unauthorized, "401", false, false)]
    [InlineData(HttpStatusCode.TooManyRequests, "429", true, false)]
    [InlineData(HttpStatusCode.InternalServerError, "500", true, true)]
    public async Task SendAsync_MapsHttpErrors(
        HttpStatusCode status, string code, bool transient, bool ambiguous)
    {
        using var handler = new MockHttpMessageHandler();
        handler.Expect(HttpMethod.Post, "https://api.exotel.com/v1/Accounts/test-account/Sms/send.json")
            .Respond(status, "application/json", """{"message":"Sensitive provider text"}""");

        var result = await BuildProvider(handler, ExotelRegion.Singapore, false).SendAsync(
            new SmsMessage { To = "+919876543210", Body = "Hello" });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(code);
        result.IsTransientFailure.Should().Be(transient);
        result.MayHaveBeenAccepted.Should().Be(ambiguous);
        result.ErrorMessage.Should().NotContain("Sensitive provider text");
    }

    private static ExotelSmsProvider BuildProvider(MockHttpMessageHandler handler, ExotelRegion region, bool includeDlt)
    {
        var options = new ExotelOptions
        {
            AccountSid = "test-account",
            ApiKey = "test-key",
            AuthToken = "test-token",
            From = "EXOTEL",
            Region = region,
            DltEntityId = includeDlt ? "entity-id" : null,
            DltTemplateId = includeDlt ? "template-id" : null
        };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(HttpClientNames.Exotel).Returns(handler.ToHttpClient());
        return new ExotelSmsProvider("exotel", options, factory);
    }
}
