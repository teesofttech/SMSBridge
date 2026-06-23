using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RichardSzalay.MockHttp;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.MessageBird;

namespace SmsBridge.ProviderTests;

public sealed class MessageBirdProviderTests
{
    private static readonly MessageBirdOptions Options = new()
    {
        AccessKey = "test-access-key",
        From = "SmsBridge"
    };

    [Fact]
    public async Task SendAsync_ReturnsSentResultOnSuccess()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://rest.messagebird.com/messages")
            .Respond("application/json", """
                {
                    "id": "mb-123",
                    "recipients": {
                        "items": [{"status": "sent"}]
                    }
                }
                """);

        var result = await BuildProvider(mock.ToHttpClient()).SendAsync(
            new SmsMessage { To = "+447700900001", Body = "Hi" });

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("messagebird");
        result.ProviderMessageId.Should().Be("mb-123");
        result.Status.Should().Be(SmsDeliveryStatus.Sent);
    }

    [Fact]
    public async Task SendAsync_UsesDocumentedAuthenticationAndFormBody()
    {
        var handler = new RecordingHandler();

        var result = await BuildProvider(new HttpClient(handler)).SendAsync(
            new SmsMessage
            {
                To = "+447700900001",
                Body = "Hello 👋",
                From = "CustomSender"
            });

        result.Success.Should().BeTrue();
        handler.AuthorizationScheme.Should().Be("AccessKey");
        handler.AuthorizationParameter.Should().Be("test-access-key");
        handler.Accept.Should().Contain("application/json");
        handler.ContentType.Should().Be("application/x-www-form-urlencoded");
        handler.Body.Should().Contain("originator=CustomSender");
        handler.Body.Should().Contain("recipients=447700900001");
        handler.Body.Should().Contain("body=Hello+%F0%9F%91%8B");
        handler.Body.Should().Contain("datacoding=auto");
    }

    [Fact]
    public async Task SendAsync_ReturnsNonTransientFailureOn422()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://rest.messagebird.com/messages")
            .Respond(HttpStatusCode.UnprocessableEntity, "application/json", """
                {
                    "errors": [{
                        "code": 9,
                        "description": "no (correct) recipients found",
                        "parameter": "recipients"
                    }]
                }
                """);

        var result = await BuildProvider(mock.ToHttpClient()).SendAsync(
            new SmsMessage { To = "invalid", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("9");
        result.IsTransientFailure.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_ReturnsAmbiguousTransientFailureOn503()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://rest.messagebird.com/messages")
            .Respond(HttpStatusCode.ServiceUnavailable, "application/json", "{}");

        var result = await BuildProvider(mock.ToHttpClient()).SendAsync(
            new SmsMessage { To = "+447700900001", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeTrue();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }

    private static MessageBirdSmsProvider BuildProvider(HttpClient httpClient)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(HttpClientNames.MessageBird).Returns(httpClient);

        return new MessageBirdSmsProvider(
            "messagebird",
            Options,
            factory,
            NullLogger<MessageBirdSmsProvider>.Instance);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? AuthorizationScheme { get; private set; }
        public string? AuthorizationParameter { get; private set; }
        public List<string> Accept { get; } = [];
        public string? ContentType { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            Accept.AddRange(request.Headers.Accept.Select(value => value.MediaType ?? string.Empty));
            ContentType = request.Content?.Headers.ContentType?.MediaType;
            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(
                    """{"id":"mb-123","recipients":{"items":[{"status":"sent"}]}}""",
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
