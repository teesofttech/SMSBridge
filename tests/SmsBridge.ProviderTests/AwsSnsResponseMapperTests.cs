using SmsBridge.Abstractions;
using SmsBridge.Providers.AwsSns;

namespace SmsBridge.ProviderTests;

public sealed class AwsSnsResponseMapperTests
{
    [Fact]
    public void FromResponse_MapsPublishMessageId()
    {
        const string xml = """
            <PublishResponse xmlns="https://sns.amazonaws.com/doc/2010-03-31/">
              <PublishResult>
                <MessageId>12345678-90ab-cdef-1234-567890abcdef</MessageId>
              </PublishResult>
              <ResponseMetadata>
                <RequestId>request-id</RequestId>
              </ResponseMetadata>
            </PublishResponse>
            """;

        var result = AwsSnsSmsResponseMapper.FromResponse("aws", xml);

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("aws");
        result.ProviderMessageId.Should().Be("12345678-90ab-cdef-1234-567890abcdef");
        result.Status.Should().Be(SmsDeliveryStatus.Accepted);
    }

    [Fact]
    public void FromErrorResponse_MapsAwsErrorCodeAndMessage()
    {
        const string xml = """
            <ErrorResponse xmlns="https://sns.amazonaws.com/doc/2010-03-31/">
              <Error>
                <Type>Sender</Type>
                <Code>InvalidParameter</Code>
                <Message>Invalid parameter: PhoneNumber</Message>
              </Error>
              <RequestId>request-id</RequestId>
            </ErrorResponse>
            """;

        var result = AwsSnsSmsResponseMapper.FromErrorResponse("aws", 400, xml);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("InvalidParameter");
        result.ErrorMessage.Should().Be("Invalid parameter: PhoneNumber");
        result.IsTransientFailure.Should().BeFalse();
    }

    [Fact]
    public void FromResponse_ReturnsFailureWhenMessageIdIsMissing()
    {
        const string xml = """
            <PublishResponse xmlns="https://sns.amazonaws.com/doc/2010-03-31/">
              <PublishResult />
            </PublishResponse>
            """;

        var result = AwsSnsSmsResponseMapper.FromResponse("aws", xml);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("AWS SNS response did not include a MessageId.");
    }

    [Theory]
    [InlineData(500, "InternalError")]
    [InlineData(503, "ServiceUnavailable")]
    [InlineData(400, "Throttling")]
    [InlineData(400, "Throttled")]
    public void FromErrorResponse_MarksTransientAwsFailures(int statusCode, string code)
    {
        var xml = $$"""
            <ErrorResponse xmlns="https://sns.amazonaws.com/doc/2010-03-31/">
              <Error>
                <Code>{{code}}</Code>
                <Message>Try again later</Message>
              </Error>
            </ErrorResponse>
            """;

        var result = AwsSnsSmsResponseMapper.FromErrorResponse("aws", statusCode, xml);

        result.IsTransientFailure.Should().BeTrue();
    }
}
