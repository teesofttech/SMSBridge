using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RichardSzalay.MockHttp;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.SmartSms;

namespace SmsBridge.ProviderTests;

public sealed class SmartSmsProviderTests
{
    private static readonly SmartSmsOptions Options = new()
    {
        Token = "test-token",
        From = "SmsBridge"
    };

    [Fact]
    public async Task SendAsync_ReturnsAcceptedResultOnCode1000()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://app.smartsmssolutions.com/io/api/client/v1/sms/")
            .Respond("application/json", """
                {
                    "code": 1000,
                    "message_id": "msg-20210427-KXZvZTXUicwVeKu2HSHVMjpWcdOmIzduUVw16SZ4",
                    "comment": "Completed Successfully"
                }
                """);

        var result = await BuildProvider(mock.ToHttpClient()).SendAsync(
            new SmsMessage { To = "2348012345678", Body = "Hi" });

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("smart-sms");
        result.ProviderMessageId.Should().Be("msg-20210427-KXZvZTXUicwVeKu2HSHVMjpWcdOmIzduUVw16SZ4");
        result.Status.Should().Be(SmsDeliveryStatus.Accepted);
    }

    [Fact]
    public async Task SendAsync_UsesDocumentedEndpointAndFormRequestShape()
    {
        var handler = new RecordingHandler();

        var result = await BuildProvider(new HttpClient(handler)).SendAsync(
            new SmsMessage
            {
                To = "2348012345678",
                Body = "Hello World!",
                From = "CustomSender",
                Metadata = new Dictionary<string, string>
                {
                    ["type"] = "1",
                    ["routing"] = "4",
                    ["ref_id"] = "client-ref-123",
                    ["dlr_timeout"] = "24",
                    ["schedule"] = "2026-07-04 08:10"
                }
            });

        result.Success.Should().BeTrue();
        handler.RequestUri.Should().Be("https://app.smartsmssolutions.com/io/api/client/v1/sms/");
        handler.Accept.Should().Contain("application/json");
        handler.ContentType.Should().Be("application/x-www-form-urlencoded");
        handler.Body.Should().Contain("token=test-token");
        handler.Body.Should().Contain("sender=CustomSender");
        handler.Body.Should().Contain("to=2348012345678");
        handler.Body.Should().Contain("message=Hello+World%21");
        handler.Body.Should().Contain("type=1");
        handler.Body.Should().Contain("routing=4");
        handler.Body.Should().Contain("ref_id=client-ref-123");
        handler.Body.Should().Contain("dlr_timeout=24");
        handler.Body.Should().Contain("schedule=2026-07-04+08%3A10");
    }

    [Fact]
    public async Task SendAsync_UsesConfiguredFromAndDefaultSmartSmsFields()
    {
        var handler = new RecordingHandler();

        var result = await BuildProvider(new HttpClient(handler)).SendAsync(
            new SmsMessage
            {
                To = "2348012345678",
                Body = "Hello"
            });

        result.Success.Should().BeTrue();
        handler.Body.Should().Contain("sender=SmsBridge");
        handler.Body.Should().Contain("type=0");
        handler.Body.Should().Contain("routing=3");
        handler.Body.Should().Contain("ref_id=");
    }

    [Fact]
    public async Task SendAsync_ReturnsNonTransientFailureForProviderValidationFailure()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://app.smartsmssolutions.com/io/api/client/v1/sms/")
            .Respond("application/json", """
                {
                    "success": false,
                    "comment": "Invalid token"
                }
                """);

        var result = await BuildProvider(mock.ToHttpClient()).SendAsync(
            new SmsMessage { To = "2348012345678", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid token");
        result.IsTransientFailure.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_ReturnsAmbiguousTransientFailureOn503()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://app.smartsmssolutions.com/io/api/client/v1/sms/")
            .Respond(HttpStatusCode.ServiceUnavailable, "application/json", "{}");

        var result = await BuildProvider(mock.ToHttpClient()).SendAsync(
            new SmsMessage { To = "2348012345678", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeTrue();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }

    private static SmartSmsSmsProvider BuildProvider(HttpClient httpClient)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(HttpClientNames.SmartSms).Returns(httpClient);

        return new SmartSmsSmsProvider(
            "smart-sms",
            Options,
            factory,
            NullLogger<SmartSmsSmsProvider>.Instance);
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
                        "code": 1000,
                        "message_id": "msg-20210427-KXZvZTXUicwVeKu2HSHVMjpWcdOmIzduUVw16SZ4",
                        "comment": "Completed Successfully"
                    }
                    """,
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
