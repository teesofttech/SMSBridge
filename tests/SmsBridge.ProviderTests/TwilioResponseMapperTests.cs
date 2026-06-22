using SmsBridge.Abstractions;
using SmsBridge.Providers.Twilio;

namespace SmsBridge.ProviderTests;

public sealed class TwilioResponseMapperTests
{
    [Fact]
    public void FromResponse_MapsSuccessfulQueuedResponse()
    {
        const string json = """
            {
                "sid": "SM123456",
                "status": "queued",
                "to": "+447700900001",
                "from": "+447700900000"
            }
            """;

        var result = TwilioSmsResponseMapper.FromResponse("twilio", json);

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("twilio");
        result.ProviderMessageId.Should().Be("SM123456");
        result.Status.Should().Be(SmsDeliveryStatus.Queued);
    }

    [Fact]
    public void FromResponse_MapsNumericErrorCode()
    {
        const string json = """
            {
                "sid": "SM789",
                "status": "failed",
                "error_code": 30008,
                "error_message": "Unknown error"
            }
            """;

        var result = TwilioSmsResponseMapper.FromResponse("twilio", json);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("30008");
        result.IsTransientFailure.Should().BeFalse();
        result.Status.Should().Be(SmsDeliveryStatus.Failed);
    }

    [Theory]
    [InlineData("30001")]
    [InlineData("30003")]
    public void FromResponse_MarksDocumentedRetryableDeliveryErrorsAsTransient(string errorCode)
    {
        var json = $$"""
            {
                "sid": "SM789",
                "status": "undelivered",
                "error_code": {{errorCode}},
                "error_message": "Delivery failed"
            }
            """;

        var result = TwilioSmsResponseMapper.FromResponse("twilio", json);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(errorCode);
        result.IsTransientFailure.Should().BeTrue();
        result.Status.Should().Be(SmsDeliveryStatus.Undelivered);
    }

    [Theory]
    [InlineData("30002")]
    [InlineData("30004")]
    [InlineData("30005")]
    [InlineData("30006")]
    [InlineData("30007")]
    [InlineData("30008")]
    public void FromResponse_MarksNonRetryableDeliveryErrorsAsNonTransient(string errorCode)
    {
        var json = $$"""
            {
                "sid": "SM789",
                "status": "failed",
                "error_code": {{errorCode}},
                "error_message": "Delivery failed"
            }
            """;

        var result = TwilioSmsResponseMapper.FromResponse("twilio", json);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(errorCode);
        result.IsTransientFailure.Should().BeFalse();
    }

    [Fact]
    public void FromResponse_AcceptsLegacyStringErrorCode()
    {
        const string json = """
            {
                "sid": "SM789",
                "status": "failed",
                "error_code": "30002",
                "error_message": "Account suspended"
            }
            """;

        var result = TwilioSmsResponseMapper.FromResponse("twilio", json);

        result.ErrorCode.Should().Be("30002");
        result.IsTransientFailure.Should().BeFalse();
    }

    [Fact]
    public void FromErrorResponse_MarksTransientOn5xx()
    {
        var result = TwilioSmsResponseMapper.FromErrorResponse("twilio", 503, "{}");

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeTrue();
    }

    [Fact]
    public void FromErrorResponse_MarksNonTransientOn4xx()
    {
        var result = TwilioSmsResponseMapper.FromErrorResponse("twilio", 400, "{}");

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeFalse();
    }
}
