using SmsBridge.Providers.SevenIo;

namespace SmsBridge.ProviderTests;

public sealed class SevenIoResponseMapperTests
{
    [Theory]
    [InlineData("101", true)]
    [InlineData("202", false)]
    [InlineData("305", false)]
    [InlineData("902", false)]
    public void GatewayRejection_PreservesCode(string code, bool ambiguous)
    {
        var result = SevenIoSmsResponseMapper.FromResponse("seven", $" {code}\r\n");

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(code);
        result.IsTransientFailure.Should().BeFalse();
        result.MayHaveBeenAccepted.Should().Be(ambiguous);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1000")]
    [InlineData("unexpected response")]
    public void UnexpectedResponse_SuppressesAutomaticFailover(string body)
    {
        var result = SevenIoSmsResponseMapper.FromResponse("seven", body);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().BeNull();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }
}
