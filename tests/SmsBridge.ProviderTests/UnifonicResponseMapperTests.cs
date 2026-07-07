using SmsBridge.Abstractions;
using SmsBridge.Providers.Unifonic;

namespace SmsBridge.ProviderTests;

public sealed class UnifonicResponseMapperTests
{
    [Fact]
    public void FromResponse_MapsSuccessfulSentResponse()
    {
        const string json = """
            {
                "Success": true,
                "MessageID": "msg-123",
                "Status": "Sent"
            }
            """;

        var result = UnifonicSmsResponseMapper.FromResponse("unifonic", json);

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("unifonic");
        result.ProviderMessageId.Should().Be("msg-123");
        result.Status.Should().Be(SmsDeliveryStatus.Sent);
    }

    [Fact]
    public void FromResponse_MapsProviderFailure()
    {
        const string json = """
            {
                "Success": false,
                "ErrorCode": "InvalidRecipient",
                "Message": "Invalid recipient"
            }
            """;

        var result = UnifonicSmsResponseMapper.FromResponse("unifonic", json);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("InvalidRecipient");
        result.ErrorMessage.Should().Be("Invalid recipient");
        result.IsTransientFailure.Should().BeFalse();
    }

    [Fact]
    public void FromResponse_MarksRetryStyleErrorsAsTransient()
    {
        const string json = """
            {
                "Success": false,
                "ErrorCode": "RateLimitExceeded",
                "Message": "Rate limited"
            }
            """;

        var result = UnifonicSmsResponseMapper.FromResponse("unifonic", json);

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeTrue();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }

    [Fact]
    public void FromErrorResponse_Marks503AsTransient()
    {
        var result = UnifonicSmsResponseMapper.FromErrorResponse("unifonic", 503, "{}");

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeTrue();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }

    [Fact]
    public void FromErrorResponse_Marks400AsNonTransient()
    {
        var result = UnifonicSmsResponseMapper.FromErrorResponse("unifonic", 400, "{}");

        result.Success.Should().BeFalse();
        result.IsTransientFailure.Should().BeFalse();
    }
}
