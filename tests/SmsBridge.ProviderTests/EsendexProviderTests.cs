using System.Net;
using System.Text;
using System.Xml.Linq;
using NSubstitute;
using RichardSzalay.MockHttp;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.Esendex;

namespace SmsBridge.ProviderTests;

public sealed class EsendexProviderTests
{
    private const string Url = "https://api.esendex.com/v1.0/messagedispatcher";
    private const string MessageId = "72d91006-eb01-4e69-ae98-0e02c92c8e34";

    [Theory]
    [InlineData("MyApp", "Override", "Override")]
    [InlineData("MyApp", null, "MyApp")]
    [InlineData(null, null, null)]
    public async Task SendAsync_PostsXmlWithBasicAuthentication(
        string? configuredSender, string? messageSender, string? expectedSender)
    {
        var options = MakeOptions(configuredSender);
        using var handler = new MockHttpMessageHandler();
        handler.Expect(HttpMethod.Post, Url)
            .WithHeaders("Authorization", "Basic " +
                Convert.ToBase64String(Encoding.UTF8.GetBytes("user@example.com:test-password")))
            .WithHeaders("Accept", "application/xml")
            .With(request => request.RequestUri!.Query == "" &&
                request.Content?.Headers.ContentType?.MediaType == "application/xml" &&
                request.Content.Headers.ContentType.CharSet == "utf-8")
            .WithContent(new XElement("messages",
                new XElement("accountreference", "EX0000000"),
                new XElement("message",
                    expectedSender is null ? null : new XElement("from", expectedSender),
                    new XElement("to", "+447700900123"),
                    new XElement("body", "Hi <team> & こんにちは"),
                    new XElement("characterset", "Auto")))
                .ToString(SaveOptions.DisableFormatting))
            .Respond("application/xml",
                $"""<messageheaders xmlns="http://api.esendex.com/ns/" batchid="batch"><messageheader id="{MessageId}"/></messageheaders>""");

        var result = await BuildProvider(handler, options).SendAsync(new SmsMessage
        {
            To = "+447700900123",
            From = messageSender,
            Body = "Hi <team> & こんにちは"
        });

        result.Success.Should().BeTrue();
        result.Status.Should().Be(SmsDeliveryStatus.Accepted);
        result.ProviderMessageId.Should().Be(MessageId);
        handler.VerifyNoOutstandingExpectation();
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, "403", false, false)]
    [InlineData(HttpStatusCode.TooManyRequests, "429", true, false)]
    [InlineData(HttpStatusCode.ServiceUnavailable, "503", true, true)]
    public async Task SendAsync_MapsHttpFailures(
        HttpStatusCode status, string code, bool transient, bool ambiguous)
    {
        using var handler = new MockHttpMessageHandler();
        handler.Expect(HttpMethod.Post, Url).Respond(status);

        var result = await BuildProvider(handler, MakeOptions(null)).SendAsync(
            new SmsMessage { To = "+447700900123", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(code);
        result.IsTransientFailure.Should().Be(transient);
        result.MayHaveBeenAccepted.Should().Be(ambiguous);
    }

    private static EsendexOptions MakeOptions(string? from) => new()
    {
        Username = "user@example.com",
        Password = "test-password",
        AccountReference = "EX0000000",
        From = from
    };

    private static EsendexSmsProvider BuildProvider(MockHttpMessageHandler handler, EsendexOptions options)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(HttpClientNames.Esendex).Returns(handler.ToHttpClient());
        return new EsendexSmsProvider("esendex", options, factory);
    }
}
