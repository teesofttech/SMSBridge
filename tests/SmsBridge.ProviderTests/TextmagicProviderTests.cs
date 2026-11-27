using System.Net;
using System.Text.Json;
using NSubstitute;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.Textmagic;

namespace SmsBridge.ProviderTests;

public sealed class TextmagicProviderTests
{
    [Theory]
    [InlineData(null, "MyApp")]
    [InlineData("OtherApp", "OtherApp")]
    public async Task SendAsync_SendsDocumentedV2JsonWithCredentialHeaders(
        string? senderOverride, string expectedSender)
    {
        using var handler = new RecordingHandler();
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(HttpClientNames.Textmagic).Returns(new HttpClient(handler, disposeHandler: false));
        var options = new TextmagicOptions { Username = "test-user", ApiKey = "test-key", From = "MyApp" };
        var provider = new TextmagicSmsProvider("textmagic", options, factory);

        var result = await provider.SendAsync(new SmsMessage
        {
            To = "+447700900001",
            Body = "Hello & goodbye!",
            From = senderOverride
        });

        result.Success.Should().BeTrue();
        result.Status.Should().Be(SmsDeliveryStatus.Accepted);
        result.ProviderMessageId.Should().Be("49576009");
        handler.Method.Should().Be(HttpMethod.Post);
        handler.Url.Should().Be("https://rest.textmagic.com/api/v2/messages");
        handler.Username.Should().Be(options.Username);
        handler.ApiKey.Should().Be(options.ApiKey);
        handler.MediaType.Should().Be("application/json");
        using var body = JsonDocument.Parse(handler.Body!);
        body.RootElement.GetProperty("phones").GetString().Should().Be("+447700900001");
        body.RootElement.GetProperty("text").GetString().Should().Be("Hello & goodbye!");
        body.RootElement.GetProperty("from").GetString().Should().Be(expectedSender);
        handler.Body.Should().NotContain(options.ApiKey);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public string? Url { get; private set; }
        public string? Username { get; private set; }
        public string? ApiKey { get; private set; }
        public string? MediaType { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Url = request.RequestUri?.AbsoluteUri;
            Username = request.Headers.GetValues("X-TM-Username").Single();
            ApiKey = request.Headers.GetValues("X-TM-Key").Single();
            MediaType = request.Content!.Headers.ContentType?.MediaType;
            Body = await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(
                    """{"id":49576009,"type":"message","sessionId":34436600,"messageId":49576009}""")
            };
        }
    }
}
