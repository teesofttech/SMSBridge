using SmsBridge.Providers.Textmagic;

namespace SmsBridge.ProviderTests;

public sealed class TextmagicResponseMapperTests
{
    [Theory]
    [InlineData("""{"type":"message","id":42,"sessionId":7}""", "42")]
    [InlineData("""{"type":"message","id":42,"messageId":43,"sessionId":7}""", "43")]
    [InlineData("""{"type":"session","id":7,"sessionId":7,"messageId":null}""", null)]
    [InlineData("""{"type":"bulk","id":7,"bulkId":7,"messageId":null}""", null)]
    public void Success_PreservesOnlyMessageIdentifiers(string body, string? expectedId)
    {
        var result = TextmagicSmsResponseMapper.FromResponse("textmagic", body);

        result.Success.Should().BeTrue();
        result.ProviderMessageId.Should().Be(expectedId);
    }

    [Theory]
    [InlineData("not JSON")]
    [InlineData("[]")]
    [InlineData("""{"type":"message","id":0,"sessionId":42}""")]
    [InlineData("""{"id":42}""")]
    public void UnexpectedSuccessResponse_IsAnAmbiguousFailure(string body)
    {
        var result = TextmagicSmsResponseMapper.FromResponse("textmagic", body);

        result.Success.Should().BeFalse();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }

    [Theory]
    [InlineData(400, """{"code":4,"message":"Sensitive provider text"}""", "4", false, false)]
    [InlineData(401, """{"code":401}""", "401", false, false)]
    [InlineData(429, "not JSON", "429", true, false)]
    [InlineData(503, "{}", "503", true, true)]
    public void HttpFailure_UsesHttpStatusForRetryAndDoesNotEchoProviderText(
        int status, string body, string errorCode, bool transient, bool ambiguous)
    {
        var result = TextmagicSmsResponseMapper.FromHttpError("textmagic", status, body);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(errorCode);
        result.IsTransientFailure.Should().Be(transient);
        result.MayHaveBeenAccepted.Should().Be(ambiguous);
        result.ErrorMessage.Should().NotContain("Sensitive provider text");
    }
}
