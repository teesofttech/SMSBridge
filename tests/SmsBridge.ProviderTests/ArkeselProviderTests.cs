using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.Arkesel;

namespace SmsBridge.ProviderTests;

public sealed class ArkeselProviderTests
{
    private static readonly ArkeselOptions Options = new()
    {
        ApiKey = "test-key",
        Sender = "MyApp",
        UseCase = "transactional"
    };

    [Fact]
    public async Task SendAsync_PostsV2JsonAndAuthenticatesInTheHeader()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK,
            """{"status":"success","data":[{"recipient":"2348012345678","id":"msg-123"}]}""");
        var result = await BuildProvider(handler).SendAsync(new SmsMessage
        {
            To = "+2348012345678",
            From = "OtherApp",
            Body = "Hello & goodbye? test"
        });

        result.Success.Should().BeTrue();
        result.ProviderMessageId.Should().Be("msg-123");
        handler.Method.Should().Be(HttpMethod.Post);
        handler.RequestUri!.AbsoluteUri.Should().Be("https://sms.arkesel.com/api/v2/sms/send");
        handler.ApiKey.Should().Be(Options.ApiKey);
        handler.ContentType.Should().Be("application/json");
        using var json = JsonDocument.Parse(handler.Body!);
        var payload = json.RootElement;
        payload.GetProperty("sender").GetString().Should().Be("OtherApp");
        payload.GetProperty("message").GetString().Should().Be("Hello & goodbye? test");
        payload.GetProperty("recipients").EnumerateArray().Select(value => value.GetString())
            .Should().Equal("2348012345678");
        payload.GetProperty("use_case").GetString().Should().Be("transactional");
        handler.Body.Should().NotContain(Options.ApiKey);
    }

    [Fact]
    public async Task SendAsync_ReturnsProviderFailureForDocumentedCode()
    {
        var handler = new RecordingHandler(HttpStatusCode.UnprocessableEntity,
            """{"status":"error","message":"The message field is required."}""");
        var result = await BuildProvider(handler).SendAsync(
            new SmsMessage { To = "+233123456789", Body = "Hi" });
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("422");
    }

    [Fact]
    public async Task SendAsync_UsesConfiguredSenderWhenMessageSenderMissing()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, """{"status":"success","message":"SMS accepted"}""");
        var options = new ArkeselOptions { ApiKey = "test-key", Sender = "MyApp" };
        await BuildProvider(handler, options).SendAsync(new SmsMessage
        {
            To = "+233123456789",
            Body = "Hi"
        });
        using var json = JsonDocument.Parse(handler.Body!);
        json.RootElement.GetProperty("sender").GetString().Should().Be("MyApp");
        json.RootElement.TryGetProperty("use_case", out _).Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_Http503_ReturnsTransientFailure()
    {
        var handler = new RecordingHandler(HttpStatusCode.ServiceUnavailable, "{}");
        var result = await BuildProvider(handler).SendAsync(
            new SmsMessage { To = "+233123456789", Body = "Hi" });
        result.IsTransientFailure.Should().BeTrue();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }

    private static ArkeselSmsProvider BuildProvider(HttpMessageHandler handler, ArkeselOptions? options = null)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(HttpClientNames.Arkesel).Returns(new HttpClient(handler));
        return new ArkeselSmsProvider("arkesel", options ?? Options, factory,
            NullLogger<ArkeselSmsProvider>.Instance);
    }

    private sealed class RecordingHandler(HttpStatusCode status, string responseBody) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public HttpMethod? Method { get; private set; }
        public string? ApiKey { get; private set; }
        public string? ContentType { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            Method = request.Method;
            ApiKey = request.Headers.GetValues("api-key").Single();
            ContentType = request.Content?.Headers.ContentType?.MediaType;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(responseBody)
            };
        }
    }
}
