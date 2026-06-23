using SmsBridge.Abstractions;
using SmsBridge.Providers.Infobip;

namespace SmsBridge.ProviderTests;

public sealed class InfobipResponseMapperTests
{
    [Fact]
    public void FromResponse_MapsPendingMessageAsQueued()
    {
        const string json = """
            {
                "bulkId": "bulk-123",
                "messages": [
                    {
                        "messageId": "message-123",
                        "status": {
                            "groupId": 1,
                            "groupName": "PENDING",
                            "id": 26,
                            "name": "PENDING_ACCEPTED",
                            "description": "Message sent to next instance"
                        },
                        "destination": "447700900001"
                    }
                ]
            }
            """;

        var result = InfobipSmsResponseMapper.FromResponse("infobip", json);

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("infobip");
        result.ProviderMessageId.Should().Be("message-123");
        result.Status.Should().Be(SmsDeliveryStatus.Queued);
    }

    [Fact]
    public void FromResponse_MapsDeliveredMessage()
    {
        const string json = """
            {
                "messages": [{
                    "messageId": "message-456",
                    "status": {
                        "groupName": "DELIVERED",
                        "id": 5,
                        "name": "DELIVERED_TO_HANDSET"
                    }
                }]
            }
            """;

        var result = InfobipSmsResponseMapper.FromResponse("infobip", json);

        result.Success.Should().BeTrue();
        result.Status.Should().Be(SmsDeliveryStatus.Delivered);
    }

    [Theory]
    [InlineData("REJECTED", SmsDeliveryStatus.Failed)]
    [InlineData("EXPIRED", SmsDeliveryStatus.Undelivered)]
    [InlineData("UNDELIVERABLE", SmsDeliveryStatus.Undelivered)]
    public void FromResponse_MapsFailureStatus(
        string groupName,
        SmsDeliveryStatus expectedStatus)
    {
        var json = $$"""
            {
                "messages": [{
                    "messageId": "message-789",
                    "status": {
                        "groupName": "{{groupName}}",
                        "id": 8,
                        "name": "STATUS_NAME",
                        "description": "Message could not be delivered"
                    }
                }]
            }
            """;

        var result = InfobipSmsResponseMapper.FromResponse("infobip", json);

        result.Success.Should().BeFalse();
        result.Status.Should().Be(expectedStatus);
        result.ErrorCode.Should().Be("8");
        result.ErrorMessage.Should().Be("Message could not be delivered");
    }

    [Fact]
    public void FromErrorResponse_ParsesCurrentApiError()
    {
        const string json = """
            {
                "errorCode": "E400",
                "description": "Request cannot be processed.",
                "action": "Check the request.",
                "violations": [],
                "resources": []
            }
            """;

        var result = InfobipSmsResponseMapper.FromErrorResponse("infobip", 400, json);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("E400");
        result.ErrorMessage.Should().Be("Request cannot be processed.");
        result.IsTransientFailure.Should().BeFalse();
    }

    [Fact]
    public void FromErrorResponse_Marks429AsSafeTransientFailure()
    {
        var result = InfobipSmsResponseMapper.FromErrorResponse("infobip", 429, "{}");

        result.IsTransientFailure.Should().BeTrue();
        result.MayHaveBeenAccepted.Should().BeFalse();
    }

    [Fact]
    public void FromErrorResponse_Marks5xxAsAmbiguousTransientFailure()
    {
        var result = InfobipSmsResponseMapper.FromErrorResponse("infobip", 503, "{}");

        result.IsTransientFailure.Should().BeTrue();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }
}
