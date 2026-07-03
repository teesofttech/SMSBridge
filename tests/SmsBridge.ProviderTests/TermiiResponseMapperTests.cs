using SmsBridge.Abstractions;
using SmsBridge.Providers.Termii;

namespace SmsBridge.ProviderTests;

public sealed class TermiiResponseMapperTests
{
    [Fact]
    public void FromResponse_MapsOkCodeAsAccepted()
    {
        const string json = """
            {
                "code": "ok",
                "balance": 1047.57,
                "message_id": "3017544054459083819856413",
                "message": "Successfully Sent",
                "user": "Oluwatobiloba Fatunde",
                "message_id_str": "3017544054459083819856413"
            }
            """;

        var result = TermiiSmsResponseMapper.FromResponse("termii", json);

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("termii");
        result.ProviderMessageId.Should().Be("3017544054459083819856413");
        result.Status.Should().Be(SmsDeliveryStatus.Accepted);
    }

    [Fact]
    public void FromResponse_MapsProviderFailureAsNonTransient()
    {
        const string json = """
            {
                "code": "bad_request",
                "message": "Invalid sender ID"
            }
            """;

        var result = TermiiSmsResponseMapper.FromResponse("termii", json);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("bad_request");
        result.ErrorMessage.Should().Be("Invalid sender ID");
        result.IsTransientFailure.Should().BeFalse();
    }

    [Fact]
    public void FromResponse_MapsProviderTransientCodeAsTransient()
    {
        const string json = """
            {
                "code": "timeout",
                "message": "Request timed out"
            }
            """;

        var result = TermiiSmsResponseMapper.FromResponse("termii", json);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("timeout");
        result.ErrorMessage.Should().Be("Request timed out");
        result.IsTransientFailure.Should().BeTrue();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }

    [Fact]
    public void FromErrorResponse_ParsesErrorMessage()
    {
        const string json = """
            {
                "code": "unauthorized",
                "message": "Invalid API key"
            }
            """;

        var result = TermiiSmsResponseMapper.FromErrorResponse("termii", 401, json);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("unauthorized");
        result.ErrorMessage.Should().Be("Invalid API key");
        result.IsTransientFailure.Should().BeFalse();
    }

    [Fact]
    public void FromErrorResponse_Marks5xxAsAmbiguousTransientFailure()
    {
        var result = TermiiSmsResponseMapper.FromErrorResponse("termii", 503, "{}");

        result.IsTransientFailure.Should().BeTrue();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }
}
