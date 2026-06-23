using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RichardSzalay.MockHttp;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.Infobip;

namespace SmsBridge.ProviderTests;

public sealed class InfobipProviderTests
{
    private static readonly InfobipOptions Options = new()
    {
        ApiKey = "test-api-key",
        BaseUrl = "https://example.api.infobip.com",
        From = "SmsBridge"
    };

    [Fact]
    public async Task SendAsync_ReturnsQueuedResultOnSuccess()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://example.api.infobip.com/sms/3/messages")
            .Respond("application/json", """
                {
                    "messages": [{
                        "messageId": "message-123",
                        "status": {
                            "groupName": "PENDING",
                            "id": 26,
                            "name": "PENDING_ACCEPTED"
                        }
                    }]
                }
                """);

        var result = await BuildProvider(mock.ToHttpClient()).SendAsync(
            new SmsMessage { To = "+447700900001", Body = "Hi" });

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("infobip");
        result.ProviderMessageId.Should().Be("message-123");
        result.Status.Should().Be(SmsDeliveryStatus.Queued);
    }

    [Fact]
    public async Task SendAsync_UsesCurrentEndpointAuthenticationAndRequestShape()
    {
        var handler = new RecordingHandler();

        var result = await BuildProvider(new HttpClient(handler)).SendAsync(
            new SmsMessage
            {
                To = "+447700900001",
                Body = "Hello",
                From = "CustomSender"
            });

        result.Success.Should().BeTrue();
        handler.RequestUri.Should().Be("https://example.api.infobip.com/sms/3/messages");
        handler.AuthorizationScheme.Should().Be("App");
        handler.AuthorizationParameter.Should().Be("test-api-key");
        handler.Accept.Should().Contain("application/json");
        handler.ContentType.Should().Be("application/json");
        handler.Body.Should().Contain("\"sender\":\"CustomSender\"");
        handler.Body.Should().Contain("\"to\":\"447700900001\"");
        handler.Body.Should().Contain("\"content\":{\"text\":\"Hello\"}");
    }

    [Fact]
    public async Task SendAsync_ReturnsNonTransientFailureOn400()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://example.api.infobip.com/sms/3/messages")
            .Respond(HttpStatusCode.BadRequest, "application/json", """
                {
                    "errorCode": "E400",
                    "description": "Request cannot be processed.",
                    "action": "Check the request.",
                    "violations": [],
                    "resources": []
                }
                """);

        var result = await BuildProvider(mock.ToHttpClient()).SendAsync(
            new SmsMessage { To = "invalid", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("E400");
        result.IsTransientFailure.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_ReturnsAmbiguousTransientFailureOn503()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://example.api.infobip.com/sms/3/messages")
            .Respond(HttpStatusCode.ServiceUnavailable, "application/json", "{}");

        var result = await BuildProvider(mock.ToHttpClient()).SendAsync(
            new SmsMessage { To = "+447700900001", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeTrue();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }

    private static InfobipSmsProvider BuildProvider(HttpClient httpClient)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(HttpClientNames.Infobip).Returns(httpClient);

        return new InfobipSmsProvider(
            "infobip",
            Options,
            factory,
            NullLogger<InfobipSmsProvider>.Instance);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? RequestUri { get; private set; }
        public string? AuthorizationScheme { get; private set; }
        public string? AuthorizationParameter { get; private set; }
        public List<string> Accept { get; } = [];
        public string? ContentType { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri?.ToString();
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
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
                        "messages": [{
                            "messageId": "message-123",
                            "status": {"groupName": "PENDING"}
                        }]
                    }
                    """,
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
