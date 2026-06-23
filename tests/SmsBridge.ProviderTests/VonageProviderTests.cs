using System.Net;
using RichardSzalay.MockHttp;
using NSubstitute;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.Vonage;
using Microsoft.Extensions.Logging.Abstractions;

namespace SmsBridge.ProviderTests;

public sealed class VonageProviderTests
{
    private static readonly VonageOptions Options = new()
    {
        ApiKey = "key123",
        ApiSecret = "secret456",
        From = "MyApp"
    };

    private static VonageSmsProvider BuildProvider(MockHttpMessageHandler mock)
    {
        var httpClient = mock.ToHttpClient();
        return BuildProvider(httpClient);
    }

    private static VonageSmsProvider BuildProvider(HttpClient httpClient)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(HttpClientNames.Vonage).Returns(httpClient);
        return new VonageSmsProvider("vonage", Options, factory, NullLogger<VonageSmsProvider>.Instance);
    }

    [Fact]
    public async Task SendAsync_ReturnsSentResultOnSuccess()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://rest.nexmo.com/sms/json")
            .Respond("application/json", """
            {
                "messages": [{"status":"0","message-id":"V001"}]
            }
            """);

        var provider = BuildProvider(mock);
        var result = await provider.SendAsync(new SmsMessage { To = "+447700900001", Body = "Hi" });

        result.Success.Should().BeTrue();
        result.ProviderMessageId.Should().Be("V001");
    }

    [Fact]
    public async Task SendAsync_UsesFormUrlEncodedRequestBody()
    {
        var handler = new RecordingHandler();
        var provider = BuildProvider(new HttpClient(handler));

        var result = await provider.SendAsync(new SmsMessage
        {
            To = "+447700900001",
            Body = "Hello & goodbye"
        });

        result.Success.Should().BeTrue();
        handler.ContentType.Should().Be("application/x-www-form-urlencoded");
        handler.Body.Should().Contain("from=MyApp");
        handler.Body.Should().Contain("to=%2B447700900001");
        handler.Body.Should().Contain("text=Hello+%26+goodbye");
        handler.Body.Should().Contain("api_key=key123");
        handler.Body.Should().Contain("api_secret=secret456");
    }

    [Fact]
    public async Task SendAsync_ReturnsFailureOnErrorStatus()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://rest.nexmo.com/sms/json")
            .Respond("application/json", """
            {
                "messages": [{"status":"4","error-text":"Bad credentials"}]
            }
            """);

        var provider = BuildProvider(mock);
        var result = await provider.SendAsync(new SmsMessage { To = "+1", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Bad credentials");
    }

    [Fact]
    public async Task SendAsync_ReturnsTransientFailureOn5xx()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://rest.nexmo.com/sms/json")
            .Respond(HttpStatusCode.ServiceUnavailable, "application/json", "{}");

        var provider = BuildProvider(mock);
        var result = await provider.SendAsync(new SmsMessage { To = "+1", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeTrue();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_SetsUnicodeTypeForNonGsmCharacters()
    {
        var handler = new RecordingHandler();
        var provider = BuildProvider(new HttpClient(handler));

        var result = await provider.SendAsync(new SmsMessage
        {
            To = "+447700900001",
            Body = "Hello 👋"
        });

        result.Success.Should().BeTrue();
        handler.Body.Should().Contain("type=unicode");
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? ContentType { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ContentType = request.Content?.Headers.ContentType?.MediaType;
            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"messages":[{"status":"0","message-id":"V001"}]}""",
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
