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
    public void FromResponse_MapsFailedResponse()
    {
        const string json = """
            {
                "sid": "SM789",
                "status": "failed",
                "error_code": "30008",
                "error_message": "Unknown error"
            }
            """;

        var result = TwilioSmsResponseMapper.FromResponse("twilio", json);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("30008");
        result.Status.Should().Be(SmsDeliveryStatus.Failed);
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
