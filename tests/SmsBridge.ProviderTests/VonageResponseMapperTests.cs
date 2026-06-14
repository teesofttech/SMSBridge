using SmsBridge.Abstractions;
using SmsBridge.Providers.Vonage;

namespace SmsBridge.ProviderTests;

public sealed class VonageResponseMapperTests
{
    [Fact]
    public void FromResponse_MapsSuccessfulResponse()
    {
        const string json = """
            {
                "messages": [
                    {
                        "status": "0",
                        "message-id": "V-MSG-001",
                        "to": "447700900001"
                    }
                ]
            }
            """;

        var result = VonageSmsResponseMapper.FromResponse("vonage", json);

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("vonage");
        result.ProviderMessageId.Should().Be("V-MSG-001");
        result.Status.Should().Be(SmsDeliveryStatus.Accepted);
    }

    [Fact]
    public void FromResponse_MapsFailureResponse()
    {
        const string json = """
            {
                "messages": [
                    {
                        "status": "4",
                        "error-text": "Bad credentials"
                    }
                ]
            }
            """;

        var result = VonageSmsResponseMapper.FromResponse("vonage", json);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("4");
        result.ErrorMessage.Should().Be("Bad credentials");
    }

    [Fact]
    public void FromResponse_MarksThrottledAsTransient()
    {
        const string json = """
            {
                "messages": [
                    {
                        "status": "1",
                        "error-text": "Throttled"
                    }
                ]
            }
            """;

        var result = VonageSmsResponseMapper.FromResponse("vonage", json);

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeTrue();
    }

    [Fact]
    public void FromResponse_ReturnsFailureForEmptyMessages()
    {
        const string json = """{ "messages": [] }""";

        var result = VonageSmsResponseMapper.FromResponse("vonage", json);

        result.Success.Should().BeFalse();
    }
}
