using SmsBridge.Providers.Esendex;

namespace SmsBridge.ProviderTests;

public sealed class EsendexResponseMapperTests
{
    [Fact]
    public void MultipleHeaders_AreAcceptedWithoutPickingAnArbitraryId()
    {
        var result = EsendexSmsResponseMapper.FromResponse("esendex", """
            <api:messageheaders xmlns:api="http://api.esendex.com/ns/">
                <api:messageheader id="72d91006-eb01-4e69-ae98-0e02c92c8e34"/>
                <api:messageheader id="0ed72db5-3934-41c6-b7ff-191bc056cee8"/>
            </api:messageheaders>
            """);

        result.Success.Should().BeTrue();
        result.ProviderMessageId.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("<messageheaders")]
    [InlineData("""<messageheaders xmlns="http://api.esendex.com/ns/" batchid="72d91006-eb01-4e69-ae98-0e02c92c8e34"/>""")]
    [InlineData("""<messageheaders xmlns="http://api.esendex.com/ns/"><messageheader id="invalid"/></messageheaders>""")]
    [InlineData("""<messageheaders xmlns="http://api.esendex.com/ns/"><messageheader/></messageheaders>""")]
    public void UnexpectedAcknowledgement_IsAnAmbiguousFailure(string body)
    {
        var result = EsendexSmsResponseMapper.FromResponse("esendex", body);

        result.Success.Should().BeFalse();
        result.ProviderMessageId.Should().BeNull();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }
}
