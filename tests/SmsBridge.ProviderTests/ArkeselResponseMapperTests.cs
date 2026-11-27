using SmsBridge.Abstractions;
using SmsBridge.Providers.Arkesel;

namespace SmsBridge.ProviderTests;

public sealed class ArkeselResponseMapperTests
{
    private const string Recipient = "+233123456789";

    [Fact]
    public void Success_MapsReferenceForRequestedRecipient()
    {
        var result = Map("""{"status":"success","data":[{"recipient":"233999999999","id":"other"},{"recipient":"233123456789","id":"msg-123"}]}""");
        result.Success.Should().BeTrue();
        result.ProviderMessageId.Should().Be("msg-123");
        result.Status.Should().Be(SmsDeliveryStatus.Accepted);
    }

    [Fact]
    public void SuccessMessage_IsAcceptedWithoutReference()
    {
        var result = Map("""{"status":"success","message":"SMS accepted"}""");
        result.Success.Should().BeTrue();
        result.ProviderMessageId.Should().BeNull();
    }

    [Fact]
    public void SuccessEnvelope_WithRejectedRecipient_ReturnsFailure()
    {
        var result = Map("""{"status":"success","data":[{"invalid numbers":["233123456789"]}]}""");
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("invalid_recipient");
        result.MayHaveBeenAccepted.Should().BeFalse();
        result.ErrorMessage.Should().NotContain("233123456789");
    }

    [Fact]
    public void ErrorEnvelope_WithHttpSuccess_ReturnsFailure()
    {
        var result = Map("""{"status":"error","message":"Sensitive provider text"}""");
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("error");
        result.ErrorMessage.Should().NotContain("Sensitive provider text");
        result.IsTransientFailure.Should().BeFalse();
    }

    [Theory]
    [InlineData("OK")]
    [InlineData("[]")]
    [InlineData("""{"status":"success"}""")]
    [InlineData("""{"status":"success","data":{}}""")]
    [InlineData("""{"status":"success","data":[]}""")]
    [InlineData("""{"status":"success","data":[{"recipient":"233999999999","id":"other"}]}""")]
    public void UnexpectedSuccessResponse_IsAmbiguousFailure(string body)
    {
        var result = Map(body);
        result.Success.Should().BeFalse();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }

    [Theory]
    [InlineData(401, false, false)]
    [InlineData(402, false, false)]
    [InlineData(403, false, false)]
    [InlineData(422, false, false)]
    [InlineData(429, true, false)]
    [InlineData(500, true, true)]
    [InlineData(503, true, true)]
    public void HttpFailure_PreservesStatusAndRetrySemantics(int status, bool transient, bool mayHaveBeenAccepted)
    {
        var result = ArkeselSmsResponseMapper.FromErrorResponse("arkesel", status);
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(status.ToString());
        result.IsTransientFailure.Should().Be(transient);
        result.MayHaveBeenAccepted.Should().Be(mayHaveBeenAccepted);
    }

    private static SmsSendResult Map(string body) =>
        ArkeselSmsResponseMapper.FromResponse("arkesel", Recipient, body);
}
