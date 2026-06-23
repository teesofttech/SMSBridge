using SmsBridge.Abstractions;
using SmsBridge.Providers.MessageBird;

namespace SmsBridge.ProviderTests;

public sealed class MessageBirdResponseMapperTests
{
    [Fact]
    public void FromResponse_MapsSentMessage()
    {
        const string json = """
            {
                "id": "mb-123",
                "recipients": {
                    "items": [
                        {
                            "recipient": 447700900001,
                            "status": "sent",
                            "statusReason": "pending DLR",
                            "statusErrorCode": null
                        }
                    ]
                }
            }
            """;

        var result = MessageBirdSmsResponseMapper.FromResponse("messagebird", json);

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("messagebird");
        result.ProviderMessageId.Should().Be("mb-123");
        result.Status.Should().Be(SmsDeliveryStatus.Sent);
    }

    [Fact]
    public void FromResponse_MapsBufferedMessageAsQueued()
    {
        const string json = """
            {
                "id": "mb-456",
                "recipients": {
                    "items": [{"status": "buffered"}]
                }
            }
            """;

        var result = MessageBirdSmsResponseMapper.FromResponse("messagebird", json);

        result.Success.Should().BeTrue();
        result.Status.Should().Be(SmsDeliveryStatus.Queued);
    }

    [Fact]
    public void FromResponse_MapsDeliveryFailure()
    {
        const string json = """
            {
                "id": "mb-789",
                "recipients": {
                    "items": [
                        {
                            "status": "delivery_failed",
                            "statusReason": "unknown subscriber",
                            "statusErrorCode": 1
                        }
                    ]
                }
            }
            """;

        var result = MessageBirdSmsResponseMapper.FromResponse("messagebird", json);

        result.Success.Should().BeFalse();
        result.ProviderMessageId.Should().BeNull();
        result.Status.Should().Be(SmsDeliveryStatus.Failed);
        result.ErrorCode.Should().Be("1");
        result.ErrorMessage.Should().Be("unknown subscriber");
        result.MayHaveBeenAccepted.Should().BeTrue();
    }

    [Fact]
    public void FromErrorResponse_ParsesStructuredApiError()
    {
        const string json = """
            {
                "errors": [
                    {
                        "code": 9,
                        "description": "no (correct) recipients found",
                        "parameter": "recipients"
                    }
                ]
            }
            """;

        var result = MessageBirdSmsResponseMapper.FromErrorResponse("messagebird", 422, json);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("9");
        result.ErrorMessage.Should().Be("no (correct) recipients found");
        result.IsTransientFailure.Should().BeFalse();
    }

    [Fact]
    public void FromErrorResponse_Marks429AsTransientAndSafeForFailover()
    {
        var result = MessageBirdSmsResponseMapper.FromErrorResponse("messagebird", 429, "{}");

        result.IsTransientFailure.Should().BeTrue();
        result.MayHaveBeenAccepted.Should().BeFalse();
    }

    [Fact]
    public void FromErrorResponse_Marks5xxAsAmbiguous()
    {
        var result = MessageBirdSmsResponseMapper.FromErrorResponse("messagebird", 503, "{}");

        result.IsTransientFailure.Should().BeTrue();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }
}
