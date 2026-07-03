using SmsBridge.Abstractions;
using SmsBridge.Providers.SmartSms;

namespace SmsBridge.ProviderTests;

public sealed class SmartSmsResponseMapperTests
{
    [Fact]
    public void FromResponse_MapsCode1000AsAccepted()
    {
        const string json = """
            {
                "code": 1000,
                "message_id": "msg-20210427-KXZvZTXUicwVeKu2HSHVMjpWcdOmIzduUVw16SZ4",
                "comment": "Completed Successfully"
            }
            """;

        var result = SmartSmsSmsResponseMapper.FromResponse("smart-sms", json);

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("smart-sms");
        result.ProviderMessageId.Should().Be("msg-20210427-KXZvZTXUicwVeKu2HSHVMjpWcdOmIzduUVw16SZ4");
        result.Status.Should().Be(SmsDeliveryStatus.Accepted);
    }

    [Fact]
    public void FromResponse_MapsProviderValidationFailureAsNonTransient()
    {
        const string json = """
            {
                "success": false,
                "comment": "Invalid token"
            }
            """;

        var result = SmartSmsSmsResponseMapper.FromResponse("smart-sms", json);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("success:false");
        result.ErrorMessage.Should().Be("Invalid token");
        result.IsTransientFailure.Should().BeFalse();
    }

    [Fact]
    public void FromResponse_MapsProvider5xxxCodeAsTransient()
    {
        const string json = """
            {
                "code": "5001",
                "message": "Temporary provider failure"
            }
            """;

        var result = SmartSmsSmsResponseMapper.FromResponse("smart-sms", json);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("5001");
        result.ErrorMessage.Should().Be("Temporary provider failure");
        result.IsTransientFailure.Should().BeTrue();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }

    [Fact]
    public void FromErrorResponse_ParsesErrorTitle()
    {
        const string json = """
            {
                "errors": [{
                    "title": "Resource not found",
                    "code": 404
                }]
            }
            """;

        var result = SmartSmsSmsResponseMapper.FromErrorResponse("smart-sms", 404, json);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Resource not found");
        result.IsTransientFailure.Should().BeFalse();
    }

    [Fact]
    public void FromErrorResponse_Marks5xxAsAmbiguousTransientFailure()
    {
        var result = SmartSmsSmsResponseMapper.FromErrorResponse("smart-sms", 503, "{}");

        result.IsTransientFailure.Should().BeTrue();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }
}
