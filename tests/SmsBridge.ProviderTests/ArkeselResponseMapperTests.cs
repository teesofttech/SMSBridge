using SmsBridge.Abstractions;
using SmsBridge.Providers.Arkesel;

namespace SmsBridge.ProviderTests;

public sealed class ArkeselResponseMapperTests
{
    [Fact]
    public void PlainOk_IsAcceptedWithoutMessageId()
    {
        var result = ArkeselSmsResponseMapper.FromResponse("arkesel", "OK");
        result.Success.Should().BeTrue();
        result.ProviderMessageId.Should().BeNull();
        result.Status.Should().Be(SmsDeliveryStatus.Accepted);
    }

    [Theory]
    [InlineData("100")]
    [InlineData("101")]
    [InlineData("102")]
    [InlineData("103")]
    [InlineData("104")]
    [InlineData("105")]
    [InlineData("106")]
    [InlineData("109")]
    [InlineData("111")]
    public void DocumentedV1ErrorCodes_AreFailures(string code)
    {
        var result = ArkeselSmsResponseMapper.FromResponse("arkesel", code);
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(code);
        result.IsTransientFailure.Should().BeFalse();
    }

    [Fact]
    public void JsonSuccess_MapsProviderReference()
    {
        var result = ArkeselSmsResponseMapper.FromResponse(
            "arkesel", """{"status":"OK","message_id":"msg-123"}""");
        result.Success.Should().BeTrue();
        result.ProviderMessageId.Should().Be("msg-123");
    }

    [Fact]
    public void UnknownSuccessResponse_IsAmbiguousFailure()
    {
        var result = ArkeselSmsResponseMapper.FromResponse("arkesel", "unrecognised response");
        result.Success.Should().BeFalse();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }

    [Fact]
    public void Http503_IsTransientAndPotentiallyAccepted()
    {
        var result = ArkeselSmsResponseMapper.FromErrorResponse("arkesel", 503, "{}");
        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeTrue();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }

    [Fact]
    public void Http401_IsNotTransient()
    {
        var result = ArkeselSmsResponseMapper.FromErrorResponse("arkesel", 401,
            """{"code":401,"message":"Invalid API key"}""");
        result.ErrorCode.Should().Be("401");
        result.ErrorMessage.Should().Be("Invalid API key");
        result.IsTransientFailure.Should().BeFalse();
    }
}
