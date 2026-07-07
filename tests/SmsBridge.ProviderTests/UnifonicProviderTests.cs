using System.Net;
using System.Net.Http.Headers;
using System.Text;
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
                    "success": "true",
                    "message": "",
                    "errorCode": "ER-00",
                    "data": {
                        "MessageID": 3200017889310,
                        "Status": "Queued"
                    }
                }
                """);

        var result = await BuildProvider(mock.ToHttpClient()).SendAsync(
            new SmsMessage { To = "+966500000000", Body = "Hi" });

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("unifonic");
        result.ProviderMessageId.Should().Be("3200017889310");
        result.Status.Should().Be(SmsDeliveryStatus.Queued);
    }

    [Fact]
    public async Task SendAsync_UsesDocumentedEndpointAndQueryRequestShape()
    {
        var handler = new RecordingHandler();

        var result = await BuildProvider(new HttpClient(handler)).SendAsync(
            new SmsMessage
            {
                To = "+966500000000",
                Body = "Hello World!",
                From = "CustomSender",
                Metadata = new Dictionary<string, string>
                {
                    ["CorrelationID"] = "client-ref-123",
                    ["statusCallback"] = "https://example.com/unifonic/status",
                    ["async"] = "true"
                }
            });

        result.Success.Should().BeTrue();
        handler.RequestUri.Should().StartWith("https://el.cloud.unifonic.com/rest/SMS/messages?");
        handler.Accept.Should().Contain("application/json");
        handler.Authorization.Should().Be(
            new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes("test-app-sid:"))).ToString());
        handler.ContentType.Should().Be("application/json");
        handler.Body.Should().Be("{}");
        handler.RequestUri.Should().Contain("AppSid=test-app-sid");
        handler.RequestUri.Should().Contain("SenderID=CustomSender");
        handler.RequestUri.Should().Contain("Body=Hello World%21");
        handler.RequestUri.Should().Contain("Recipient=966500000000");
        handler.RequestUri.Should().Contain("responseType=json");
        handler.RequestUri.Should().Contain("CorrelationID=client-ref-123");
        handler.RequestUri.Should().Contain("statusCallback=https%3A%2F%2Fexample.com%2Funifonic%2Fstatus");
        handler.RequestUri.Should().Contain("async=true");
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
        handler.RequestUri.Should().Contain("SenderID=SmsBridge");
    }

    [Fact]
    public async Task SendAsync_ReturnsFailureResultOn400()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://el.cloud.unifonic.com/rest/SMS/messages")
            .Respond(HttpStatusCode.BadRequest, "application/json", """
                {
                    "errorCode": "ER-482",
                    "message": "Invalid recipient"
                }
                """);

        var result = await BuildProvider(mock.ToHttpClient()).SendAsync(
            new SmsMessage { To = "invalid", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("ER-482");
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
        public string? Authorization { get; private set; }
        public string? ContentType { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri?.ToString();
            Accept.AddRange(request.Headers.Accept.Select(value => value.MediaType ?? string.Empty));
            Authorization = request.Headers.Authorization?.ToString();
            ContentType = request.Content?.Headers.ContentType?.MediaType;
            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                        "success": "true",
                        "message": "",
                        "errorCode": "ER-00",
                        "data": {
                            "MessageID": 3200017889310,
                            "Status": "Queued"
                        }
                    }
                    """,
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
