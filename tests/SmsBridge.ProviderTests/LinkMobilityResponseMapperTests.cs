using SmsBridge.Abstractions;
using SmsBridge.Providers.LinkMobility;

namespace SmsBridge.ProviderTests;

public sealed class LinkMobilityResponseMapperTests
{
    [Theory]
    [InlineData("""{"resultCode":1005,"messageId":"msg-123"}""")]
    [InlineData("""{"resultCode":"1005","messageId":"msg-123"}""")]
    public void QueuedResponse_ReturnsMessageId(string body)
    {
        var result = LinkMobilitySmsResponseMapper.FromResponse("link", body);

        result.Success.Should().BeTrue();
        result.Status.Should().Be(SmsDeliveryStatus.Queued);
        result.ProviderMessageId.Should().Be("msg-123");
    }

    [Theory]
    [InlineData("not JSON")]
    [InlineData("""[{"resultCode":1005,"messageId":"id"}]""")]
    [InlineData("""{"resultCode":1001,"messageId":"id"}""")]
    [InlineData("""{"resultCode":1005,"messageId":""}""")]
    public void UnexpectedAcknowledgement_IsAnAmbiguousFailure(string body)
    {
        var result = LinkMobilitySmsResponseMapper.FromResponse("link", body);

        result.Success.Should().BeFalse();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }

    [Theory]
    [InlineData(400, """{"resultCode":106200,"description":"Sensitive provider text"}""", "106200", false, false)]
    [InlineData(401, """{"resultCode":"101100"}""", "101100", false, false)]
    [InlineData(429, "", "429", true, false)]
    [InlineData(503, "not JSON", "503", true, true)]
    public void HttpFailure_UsesHttpStatusForRetryAndPreservesProviderCode(
        int status, string body, string code, bool transient, bool ambiguous)
    {
        var result = LinkMobilitySmsResponseMapper.FromHttpError("link", status, body);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(code);
        result.IsTransientFailure.Should().Be(transient);
        result.MayHaveBeenAccepted.Should().Be(ambiguous);
        result.ErrorMessage.Should().NotContain("Sensitive provider text");
    }
}
