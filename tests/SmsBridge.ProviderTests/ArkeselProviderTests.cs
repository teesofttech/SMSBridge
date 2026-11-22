using System.Net;
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
        ApiKey = "test key&secret",
        Sender = "Test Sender",
        UseCase = "transactional"
    };

    [Fact]
    public async Task SendAsync_SendsEncodedV1Parameters()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "OK");
        var result = await BuildProvider(handler).SendAsync(new SmsMessage
        {
            To = "+233123456789",
            From = "Custom Sender",
            Body = "Hello & goodbye? test"
        });

        result.Success.Should().BeTrue();
        handler.Method.Should().Be(HttpMethod.Get);
        handler.RequestUri.Should().NotBeNull();
        handler.RequestUri!.GetLeftPart(UriPartial.Path).Should().Be("https://sms.arkesel.com/sms/api");
        var query = handler.RequestUri.Query;
        query.Should().Contain("action=send-sms");
        query.Should().Contain("api_key=test%20key%26secret");
        query.Should().Contain("to=%2B233123456789");
        query.Should().Contain("from=Custom%20Sender");
        query.Should().Contain("sms=Hello%20%26%20goodbye%3F%20test");
        query.Should().Contain("use_case=transactional");
    }

    [Fact]
    public async Task SendAsync_ReturnsProviderFailureForDocumentedCode()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "101");
        var result = await BuildProvider(handler).SendAsync(
            new SmsMessage { To = "+233123456789", Body = "Hi" });
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("101");
    }

    [Fact]
    public async Task SendAsync_UsesConfiguredSenderWhenMessageSenderMissing()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "OK");
        await BuildProvider(handler).SendAsync(new SmsMessage
        {
            To = "+233123456789",
            Body = "Hi"
        });
        handler.RequestUri!.Query.Should().Contain("from=Test%20Sender");
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

    private static ArkeselSmsProvider BuildProvider(HttpMessageHandler handler)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(HttpClientNames.Arkesel).Returns(new HttpClient(handler));
        return new ArkeselSmsProvider("arkesel", Options, factory,
            NullLogger<ArkeselSmsProvider>.Instance);
    }

    private sealed class RecordingHandler(HttpStatusCode status, string responseBody) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public HttpMethod? Method { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            Method = request.Method;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(responseBody)
            });
        }
    }
}
