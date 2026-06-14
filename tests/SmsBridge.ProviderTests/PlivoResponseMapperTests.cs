using SmsBridge.Abstractions;
using SmsBridge.Providers.Plivo;

namespace SmsBridge.ProviderTests;

public sealed class PlivoResponseMapperTests
{
    [Fact]
    public void FromResponse_MapsQueuedResponse()
    {
        const string json = """{"message_uuid":["msg-123"],"message":"message(s) queued"}""";

        var result = PlivoSmsResponseMapper.FromResponse("plivo", json);

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("plivo");
        result.ProviderMessageId.Should().Be("msg-123");
        result.Status.Should().Be(SmsDeliveryStatus.Queued);
    }

    [Fact]
    public void FromResponse_HandlesEmptyUuidArray()
    {
        const string json = """{"message_uuid":[],"message":"message(s) queued"}""";

        var result = PlivoSmsResponseMapper.FromResponse("plivo", json);

        result.Success.Should().BeTrue();
        result.ProviderMessageId.Should().BeNull();
    }

    [Fact]
    public void FromErrorResponse_MarksTransientOn5xx()
    {
        var result = PlivoSmsResponseMapper.FromErrorResponse("plivo", 500, """{"error":"Internal server error"}""");

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeTrue();
        result.ErrorMessage.Should().Be("Internal server error");
    }

    [Fact]
    public void FromErrorResponse_MarksTransientOn429()
    {
        var result = PlivoSmsResponseMapper.FromErrorResponse("plivo", 429, "{}");

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeTrue();
    }

    [Fact]
    public void FromErrorResponse_MarksNonTransientOn4xx()
    {
        var result = PlivoSmsResponseMapper.FromErrorResponse("plivo", 400, """{"error":"Invalid source number"}""");

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid source number");
    }
}
