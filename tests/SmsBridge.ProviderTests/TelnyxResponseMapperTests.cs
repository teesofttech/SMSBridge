using SmsBridge.Abstractions;
using SmsBridge.Providers.Telnyx;

namespace SmsBridge.ProviderTests;

public sealed class TelnyxResponseMapperTests
{
    [Fact]
    public void FromResponse_MapsQueuedResponse()
    {
        const string json = """
            {"data":{"id":"msg-123","to":[{"status":"queued"}]}}
            """;

        var result = TelnyxSmsResponseMapper.FromResponse("telnyx", json);

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("telnyx");
        result.ProviderMessageId.Should().Be("msg-123");
        result.Status.Should().Be(SmsDeliveryStatus.Queued);
    }

    [Fact]
    public void FromResponse_MapsSentResponse()
    {
        const string json = """{"data":{"id":"msg-456","to":[{"status":"sent"}]}}""";

        var result = TelnyxSmsResponseMapper.FromResponse("telnyx", json);

        result.Success.Should().BeTrue();
        result.Status.Should().Be(SmsDeliveryStatus.Sent);
    }

    [Fact]
    public void FromResponse_MapsDeliveredResponse()
    {
        const string json = """{"data":{"id":"msg-789","to":[{"status":"delivered"}]}}""";

        var result = TelnyxSmsResponseMapper.FromResponse("telnyx", json);

        result.Success.Should().BeTrue();
        result.Status.Should().Be(SmsDeliveryStatus.Delivered);
    }

    [Fact]
    public void FromResponse_MapsFailedStatus()
    {
        const string json = """{"data":{"id":"msg-000","to":[{"status":"failed"}]}}""";

        var result = TelnyxSmsResponseMapper.FromResponse("telnyx", json);

        result.Success.Should().BeFalse();
        result.Status.Should().Be(SmsDeliveryStatus.Failed);
    }

    [Fact]
    public void FromResponse_ReturnsFailure_WhenDataMissing()
    {
        const string json = """{}""";

        var result = TelnyxSmsResponseMapper.FromResponse("telnyx", json);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public void FromErrorResponse_MarksTransientOn5xx()
    {
        var result = TelnyxSmsResponseMapper.FromErrorResponse("telnyx", 503, "{}");

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeTrue();
    }

    [Fact]
    public void FromErrorResponse_MarksTransientOn429()
    {
        var result = TelnyxSmsResponseMapper.FromErrorResponse("telnyx", 429, "{}");

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeTrue();
    }

    [Fact]
    public void FromErrorResponse_MarksNonTransientOn4xx()
    {
        const string json = """{"errors":[{"code":"10033","detail":"Invalid phone number"}]}""";

        var result = TelnyxSmsResponseMapper.FromErrorResponse("telnyx", 422, json);

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeFalse();
        result.ErrorCode.Should().Be("10033");
        result.ErrorMessage.Should().Be("Invalid phone number");
    }
}
