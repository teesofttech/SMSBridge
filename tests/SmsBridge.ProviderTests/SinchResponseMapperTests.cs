using SmsBridge.Abstractions;
using SmsBridge.Providers.Sinch;

namespace SmsBridge.ProviderTests;

public sealed class SinchResponseMapperTests
{
    [Fact]
    public void FromResponse_MapsCreatedBatchAsQueued()
    {
        const string json = """
            {
                "id": "batch-123",
                "to": ["+15551231234"],
                "from": "+15551230000",
                "body": "Hello"
            }
            """;

        var result = SinchSmsResponseMapper.FromResponse("sinch", json);

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("sinch");
        result.ProviderMessageId.Should().Be("batch-123");
        result.Status.Should().Be(SmsDeliveryStatus.Queued);
    }

    [Fact]
    public void FromResponse_DoesNotTreatBatchStatusAsDeliveryStatus()
    {
        const string json = """{"id":"batch-456","status":"Failed"}""";

        var result = SinchSmsResponseMapper.FromResponse("sinch", json);

        result.Success.Should().BeTrue();
        result.Status.Should().Be(SmsDeliveryStatus.Queued);
    }

    [Fact]
    public void FromErrorResponse_MarksTransientOn5xx()
    {
        var result = SinchSmsResponseMapper.FromErrorResponse("sinch", 503, "{}");

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeTrue();
    }

    [Fact]
    public void FromErrorResponse_MarksTransientOn429()
    {
        var result = SinchSmsResponseMapper.FromErrorResponse("sinch", 429, "{}");

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeTrue();
    }

    [Fact]
    public void FromErrorResponse_MarksNonTransientOn4xx()
    {
        const string json = """
            {
                "code": "syntax_invalid_parameter_format",
                "text": "The recipient number is invalid."
            }
            """;

        var result = SinchSmsResponseMapper.FromErrorResponse("sinch", 400, json);

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeFalse();
        result.ErrorCode.Should().Be("syntax_invalid_parameter_format");
        result.ErrorMessage.Should().Be("The recipient number is invalid.");
    }
}
