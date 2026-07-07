using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RichardSzalay.MockHttp;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.Unifonic;

namespace SmsBridge.ProviderTests;

public sealed class UnifonicProviderTests
{
    private static readonly UnifonicOptions Options = new()
    {
        AppSid = "test-app-sid",
        From = "SmsBridge"
    };

    [Fact]
    public async Task SendAsync_ReturnsAcceptedResultOnSuccess()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://el.cloud.unifonic.com/rest/SMS/messages")
            .Respond("application/json", """
                {
                    "Success": true,
                    "MessageID": "message-123",
                    "Status": "Sent"
                }
                """);

        var result = await BuildProvider(mock.ToHttpClient()).SendAsync(
            new SmsMessage { To = "+966500000000", Body = "Hi" });

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("unifonic");
        result.ProviderMessageId.Should().Be("message-123");
        result.Status.Should().Be(SmsDeliveryStatus.Sent);
    }

    [Fact]
    public async Task SendAsync_UsesDocumentedEndpointAndFormRequestShape()
    {
        var handler = new RecordingHandler();

        var result = await BuildProvider(new HttpClient(handler)).SendAsync(
            new SmsMessage
            {
                To = "+966500000000",
                Body = "Hello World!",
                From = "CustomSender"
            });

        result.Success.Should().BeTrue();
        handler.RequestUri.Should().Be("https://el.cloud.unifonic.com/rest/SMS/messages");
        handler.Accept.Should().Contain("application/json");
        handler.ContentType.Should().Be("application/x-www-form-urlencoded");
        handler.Body.Should().Contain("AppSid=test-app-sid");
        handler.Body.Should().Contain("SenderID=CustomSender");
        handler.Body.Should().Contain("Body=Hello+World%21");
        handler.Body.Should().Contain("Recipient=%2B966500000000");
    }

    [Fact]
    public async Task SendAsync_UsesConfiguredFromWhenMessageFromIsNotSupplied()
    {
        var handler = new RecordingHandler();

        var result = await BuildProvider(new HttpClient(handler)).SendAsync(
            new SmsMessage
            {
                To = "+966500000000",
                Body = "Hello"
            });

        result.Success.Should().BeTrue();
        handler.Body.Should().Contain("SenderID=SmsBridge");
    }

    [Fact]
    public async Task SendAsync_ReturnsFailureResultOn400()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://el.cloud.unifonic.com/rest/SMS/messages")
            .Respond(HttpStatusCode.BadRequest, "application/json", """
                {
                    "ErrorCode": "InvalidRecipient",
                    "Message": "Invalid recipient"
                }
                """);

        var result = await BuildProvider(mock.ToHttpClient()).SendAsync(
            new SmsMessage { To = "invalid", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("InvalidRecipient");
        result.IsTransientFailure.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_ReturnsTransientFailureOn503()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://el.cloud.unifonic.com/rest/SMS/messages")
            .Respond(HttpStatusCode.ServiceUnavailable, "application/json", "{}");

        var result = await BuildProvider(mock.ToHttpClient()).SendAsync(
            new SmsMessage { To = "+966500000000", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeTrue();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }

    private static UnifonicSmsProvider BuildProvider(HttpClient httpClient)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(HttpClientNames.Unifonic).Returns(httpClient);

        return new UnifonicSmsProvider(
            "unifonic",
            Options,
            factory,
            NullLogger<UnifonicSmsProvider>.Instance);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? RequestUri { get; private set; }
        public List<string> Accept { get; } = [];
        public string? ContentType { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri?.ToString();
            Accept.AddRange(request.Headers.Accept.Select(value => value.MediaType ?? string.Empty));
            ContentType = request.Content?.Headers.ContentType?.MediaType;
            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                        "Success": true,
                        "MessageID": "message-123",
                        "Status": "Sent"
                    }
                    """,
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
