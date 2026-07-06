using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RichardSzalay.MockHttp;
using SmsBridge.Abstractions;
using SmsBridge.Internal.Http;
using SmsBridge.Options;
using SmsBridge.Providers.AwsSns;

namespace SmsBridge.ProviderTests;

public sealed class AwsSnsProviderTests
{
    private static readonly AwsSnsOptions Options = new()
    {
        AccessKeyId = "AKIDEXAMPLE",
        SecretAccessKey = "wJalrXUtnFEMI/K7MDENG+bPxRfiCYEXAMPLEKEY",
        Region = "eu-west-2",
        SenderId = "SmsBridge"
    };

    private static AwsSnsSmsProvider BuildProvider(MockHttpMessageHandler mock)
    {
        var httpClient = mock.ToHttpClient();
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(HttpClientNames.AwsSns).Returns(httpClient);
        return new AwsSnsSmsProvider("aws", Options, factory, NullLogger<AwsSnsSmsProvider>.Instance);
    }

    [Fact]
    public async Task SendAsync_SendsSignedPublishRequest()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, "https://sns.eu-west-2.amazonaws.com/")
            .Respond(async request =>
            {
                capturedRequest = request;
                capturedBody = request.Content is null
                    ? null
                    : await request.Content.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        <PublishResponse xmlns="https://sns.amazonaws.com/doc/2010-03-31/">
                          <PublishResult>
                            <MessageId>message-123</MessageId>
                          </PublishResult>
                        </PublishResponse>
                        """)
                };
            });

        var provider = BuildProvider(mock);
        var result = await provider.SendAsync(new SmsMessage { To = "+447700900001", Body = "Hi" });

        result.Success.Should().BeTrue();
        result.ProviderMessageId.Should().Be("message-123");
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.TryGetValues("Authorization", out var authorizationValues).Should().BeTrue();
        var authorization = authorizationValues!.Single();
        authorization.Should().StartWith("AWS4-HMAC-SHA256 ");
        authorization.Should().Contain("Credential=AKIDEXAMPLE/");
        authorization.Should().Contain("/eu-west-2/sns/aws4_request");
        authorization.Should().Contain("SignedHeaders=host;x-amz-date");
        capturedRequest.Headers.Contains("x-amz-date").Should().BeTrue();
        capturedRequest.Content!.Headers.ContentType.Should().Be(new MediaTypeHeaderValue("application/x-www-form-urlencoded"));
        capturedBody.Should().Contain("Action=Publish");
        capturedBody.Should().Contain("PhoneNumber=%2B447700900001");
        capturedBody.Should().Contain("Message=Hi");
        capturedBody.Should().Contain("MessageAttributes.entry.1.Name=AWS.SNS.SMS.SenderID");
        capturedBody.Should().Contain("MessageAttributes.entry.1.Value.StringValue=SmsBridge");
    }

    [Fact]
    public async Task SendAsync_UsesMessageFromForSenderIdWhenSupplied()
    {
        string? capturedBody = null;
        var mock = new MockHttpMessageHandler();
        mock.When("https://sns.eu-west-2.amazonaws.com/")
            .Respond(async request =>
            {
                capturedBody = await request.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        <PublishResponse xmlns="https://sns.amazonaws.com/doc/2010-03-31/">
                          <PublishResult><MessageId>message-123</MessageId></PublishResult>
                        </PublishResponse>
                        """)
                };
            });

        var provider = BuildProvider(mock);
        await provider.SendAsync(new SmsMessage
        {
            To = "+447700900001",
            Body = "Hi",
            From = "Override"
        });

        capturedBody.Should().Contain("MessageAttributes.entry.1.Value.StringValue=Override");
    }

    [Fact]
    public async Task SendAsync_ReturnsFailureResultOn400()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://sns.eu-west-2.amazonaws.com/")
            .Respond(HttpStatusCode.BadRequest, "text/xml", """
                <ErrorResponse xmlns="https://sns.amazonaws.com/doc/2010-03-31/">
                  <Error>
                    <Code>InvalidParameter</Code>
                    <Message>Invalid parameter: PhoneNumber</Message>
                  </Error>
                </ErrorResponse>
                """);

        var provider = BuildProvider(mock);
        var result = await provider.SendAsync(new SmsMessage { To = "invalid", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("InvalidParameter");
        result.IsTransientFailure.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_ReturnsTransientFailureOn503()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("https://sns.eu-west-2.amazonaws.com/")
            .Respond(HttpStatusCode.ServiceUnavailable, "text/xml", """
                <ErrorResponse xmlns="https://sns.amazonaws.com/doc/2010-03-31/">
                  <Error>
                    <Code>ServiceUnavailable</Code>
                    <Message>Try again later</Message>
                  </Error>
                </ErrorResponse>
                """);

        var provider = BuildProvider(mock);
        var result = await provider.SendAsync(new SmsMessage { To = "+447700900001", Body = "Hi" });

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeTrue();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }
}
