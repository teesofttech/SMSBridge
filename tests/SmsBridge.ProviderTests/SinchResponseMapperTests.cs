using SmsBridge.Abstractions;
using SmsBridge.Providers.Sinch;

namespace SmsBridge.ProviderTests;

public sealed class SinchResponseMapperTests
{
    [Fact]
    public void FromResponse_MapsInProgressResponse()
    {
        const string json = """{"id":"batch-123","status":"In Progress"}""";

        var result = SinchSmsResponseMapper.FromResponse("sinch", json);

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("sinch");
        result.ProviderMessageId.Should().Be("batch-123");
        result.Status.Should().Be(SmsDeliveryStatus.Queued);
    }

    [Fact]
    public void FromResponse_MapsSuccessfulResponse()
    {
        const string json = """{"id":"batch-456","status":"Successful"}""";

        var result = SinchSmsResponseMapper.FromResponse("sinch", json);

        result.Success.Should().BeTrue();
        result.Status.Should().Be(SmsDeliveryStatus.Delivered);
    }

    [Fact]
    public void FromResponse_MapsFailedResponse()
    {
        const string json = """{"id":"batch-789","status":"Failed"}""";

        var result = SinchSmsResponseMapper.FromResponse("sinch", json);

        result.Success.Should().BeFalse();
        result.Status.Should().Be(SmsDeliveryStatus.Failed);
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
        var result = SinchSmsResponseMapper.FromErrorResponse("sinch", 400, "{}");

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeFalse();
    }
}
