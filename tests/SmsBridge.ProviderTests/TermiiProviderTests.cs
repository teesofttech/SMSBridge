using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RichardSzalay.MockHttp;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.Termii;

namespace SmsBridge.ProviderTests;

public sealed class TermiiProviderTests
{
    private static readonly TermiiOptions Options = new()
    {
        ApiKey = "test-api-key",
        From = "SmsBridge",
        Channel = "generic",
        BaseUrl = "https://api.ng.termii.com"
    };

    [Fact]
    public async Task SendAsync_ReturnsAcceptedResultOnOkCode()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://api.ng.termii.com/api/sms/send")
            .Respond("application/json", """
                {
                    "code": "ok",
                    "balance": 1047.57,
                    "message_id": "3017544054459083819856413",
                    "message": "Successfully Sent",
                    "user": "Oluwatobiloba Fatunde",
                    "message_id_str": "3017544054459083819856413"
                }
                """);

        var result = await BuildProvider(mock.ToHttpClient()).SendAsync(
            new SmsMessage { To = "2347015250000", Body = "Hi" });

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("termii");
        result.ProviderMessageId.Should().Be("3017544054459083819856413");
        result.Status.Should().Be(SmsDeliveryStatus.Accepted);
    }

    [Fact]
    public async Task SendAsync_UsesDocumentedEndpointAndJsonRequestShape()
    {
        var handler = new RecordingHandler();

        var result = await BuildProvider(new HttpClient(handler)).SendAsync(
            new SmsMessage
            {
                To = "2347015250000",
                Body = "Hello World!",
                From = "CustomSender",
                Metadata = new Dictionary<string, string>
                {
                    ["type"] = "unicode",
                    ["channel"] = "dnd"
                }
            });

        result.Success.Should().BeTrue();
        handler.RequestUri.Should().Be("https://api.ng.termii.com/api/sms/send");
        handler.Accept.Should().Contain("application/json");
        handler.ContentType.Should().Be("application/json");
        handler.Body.Should().Contain("\"to\":\"2347015250000\"");
        handler.Body.Should().Contain("\"from\":\"CustomSender\"");
        handler.Body.Should().Contain("\"sms\":\"Hello World!\"");
        handler.Body.Should().Contain("\"type\":\"unicode\"");
        handler.Body.Should().Contain("\"channel\":\"dnd\"");
        handler.Body.Should().Contain("\"api_key\":\"test-api-key\"");
    }

    [Fact]
    public async Task SendAsync_UsesConfiguredFromAndDefaultTermiiFields()
    {
        var handler = new RecordingHandler();

        var result = await BuildProvider(new HttpClient(handler)).SendAsync(
            new SmsMessage
            {
                To = "2347015250000",
                Body = "Hello"
            });

        result.Success.Should().BeTrue();
        handler.Body.Should().Contain("\"from\":\"SmsBridge\"");
        handler.Body.Should().Contain("\"type\":\"plain\"");
        handler.Body.Should().Contain("\"channel\":\"generic\"");
    }

    [Fact]
    public async Task SendAsync_ReturnsNonTransientFailureOn400()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://api.ng.termii.com/api/sms/send")
            .Respond(HttpStatusCode.BadRequest, "application/json", """
                {
                    "code": "bad_request",
                    "message": "Invalid request"
                }
                """);

        var result = await BuildProvider(mock.ToHttpClient()).SendAsync(
            new SmsMessage { To = "invalid", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("bad_request");
        result.ErrorMessage.Should().Be("Invalid request");
        result.IsTransientFailure.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_ReturnsAmbiguousTransientFailureOn503()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://api.ng.termii.com/api/sms/send")
            .Respond(HttpStatusCode.ServiceUnavailable, "application/json", "{}");

        var result = await BuildProvider(mock.ToHttpClient()).SendAsync(
            new SmsMessage { To = "2347015250000", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeTrue();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }

    private static TermiiSmsProvider BuildProvider(HttpClient httpClient)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(HttpClientNames.Termii).Returns(httpClient);

        return new TermiiSmsProvider(
            "termii",
            Options,
            factory,
            NullLogger<TermiiSmsProvider>.Instance);
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
                        "code": "ok",
                        "message_id": "3017544054459083819856413",
                        "message": "Successfully Sent"
                    }
                    """,
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
