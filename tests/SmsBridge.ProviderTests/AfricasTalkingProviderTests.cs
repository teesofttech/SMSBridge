using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RichardSzalay.MockHttp;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.AfricasTalking;

namespace SmsBridge.ProviderTests;

public sealed class AfricasTalkingProviderTests
{
    private static readonly AfricasTalkingOptions Options = new()
    {
        Username = "test-username",
        ApiKey = "test-api-key",
        From = "SmsBridge"
    };

    [Fact]
    public async Task SendAsync_ReturnsSentResultOnSuccess()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://api.africastalking.com/version1/messaging")
            .Respond("application/json", """
                {
                    "SMSMessageData": {
                        "Message": "Sent to 1/1 Total Cost: KES 0.8000",
                        "Recipients": [{
                            "statusCode": 101,
                            "number": "+254711XXXYYY",
                            "status": "Success",
                            "cost": "KES 0.8000",
                            "messageId": "ATPid_SampleTxnId123"
                        }]
                    }
                }
                """);

        var result = await BuildProvider(mock.ToHttpClient()).SendAsync(
            new SmsMessage { To = "+254711000000", Body = "Hi" });

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("africas-talking");
        result.ProviderMessageId.Should().Be("ATPid_SampleTxnId123");
        result.Status.Should().Be(SmsDeliveryStatus.Sent);
    }

    [Fact]
    public async Task SendAsync_UsesLegacyEndpointAuthenticationAndFormRequestShape()
    {
        var handler = new RecordingHandler();

        var result = await BuildProvider(new HttpClient(handler)).SendAsync(
            new SmsMessage
            {
                To = "+254711000000",
                Body = "Hello World!",
                From = "CustomSender",
                Metadata = new Dictionary<string, string>
                {
                    ["enqueue"] = "1",
                    ["bulk_sms_mode"] = "1",
                    ["link_id"] = "link-123"
                }
            });

        result.Success.Should().BeTrue();
        handler.RequestUri.Should().Be("https://api.africastalking.com/version1/messaging");
        handler.ApiKey.Should().Be("test-api-key");
        handler.Accept.Should().Contain("application/json");
        handler.ContentType.Should().Be("application/x-www-form-urlencoded");
        handler.Body.Should().Contain("username=test-username");
        handler.Body.Should().Contain("to=%2B254711000000");
        handler.Body.Should().Contain("message=Hello+World%21");
        handler.Body.Should().Contain("from=CustomSender");
        handler.Body.Should().Contain("enqueue=1");
        handler.Body.Should().Contain("bulkSMSMode=1");
        handler.Body.Should().Contain("linkId=link-123");
    }

    [Fact]
    public async Task SendAsync_OmitsFromWhenNoSenderIsConfigured()
    {
        var handler = new RecordingHandler();
        var options = new AfricasTalkingOptions
        {
            Username = "test-username",
            ApiKey = "test-api-key"
        };

        var result = await BuildProvider(new HttpClient(handler), options).SendAsync(
            new SmsMessage
            {
                To = "+254711000000",
                Body = "Hello"
            });

        result.Success.Should().BeTrue();
        handler.Body.Should().NotContain("from=");
    }

    [Fact]
    public async Task SendAsync_ReturnsNonTransientFailureOn400()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://api.africastalking.com/version1/messaging")
            .Respond(HttpStatusCode.BadRequest, "application/json", """
                {
                    "message": "Bad Request"
                }
                """);

        var result = await BuildProvider(mock.ToHttpClient()).SendAsync(
            new SmsMessage { To = "invalid", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Bad Request");
        result.IsTransientFailure.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_ReturnsAmbiguousTransientFailureOn503()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://api.africastalking.com/version1/messaging")
            .Respond(HttpStatusCode.ServiceUnavailable, "application/json", "{}");

        var result = await BuildProvider(mock.ToHttpClient()).SendAsync(
            new SmsMessage { To = "+254711000000", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeTrue();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }

    private static AfricasTalkingSmsProvider BuildProvider(
        HttpClient httpClient,
        AfricasTalkingOptions? options = null)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(HttpClientNames.AfricasTalking).Returns(httpClient);

        return new AfricasTalkingSmsProvider(
            "africas-talking",
            options ?? Options,
            factory,
            NullLogger<AfricasTalkingSmsProvider>.Instance);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? RequestUri { get; private set; }
        public string? ApiKey { get; private set; }
        public List<string> Accept { get; } = [];
        public string? ContentType { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri?.ToString();
            ApiKey = request.Headers.TryGetValues("apiKey", out var values)
                ? values.SingleOrDefault()
                : null;
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
                        "SMSMessageData": {
                            "Recipients": [{
                                "statusCode": 101,
                                "status": "Success",
                                "messageId": "ATPid_SampleTxnId123"
                            }]
                        }
                    }
                    """,
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
