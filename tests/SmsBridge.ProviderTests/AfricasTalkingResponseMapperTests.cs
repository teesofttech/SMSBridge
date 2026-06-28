using SmsBridge.Abstractions;
using SmsBridge.Providers.AfricasTalking;

namespace SmsBridge.ProviderTests;

public sealed class AfricasTalkingResponseMapperTests
{
    [Fact]
    public void FromResponse_MapsStatusCode101AsSent()
    {
        const string json = """
            {
                "SMSMessageData": {
                    "Message": "Sent to 1/1 Total Cost: KES 0.8000",
                    "Recipients": [{
                        "statusCode": 101,
                        "number": "+254711XXXYYY",
                        "status": "Success",
                        "cost": "KES 0.8000",
                        "messageId": "ATPid_SampleTxnId123"
                    }]
                }
            }
            """;

        var result = AfricasTalkingSmsResponseMapper.FromResponse("africas-talking", json);

        result.Success.Should().BeTrue();
        result.Provider.Should().Be("africas-talking");
        result.ProviderMessageId.Should().Be("ATPid_SampleTxnId123");
        result.Status.Should().Be(SmsDeliveryStatus.Sent);
    }

    [Theory]
    [InlineData(100, SmsDeliveryStatus.Accepted)]
    [InlineData(102, SmsDeliveryStatus.Queued)]
    public void FromResponse_MapsAcceptedButNotSentStatusesAsAmbiguousFailures(
        int statusCode,
        SmsDeliveryStatus expectedStatus)
    {
        var json = $$"""
            {
                "SMSMessageData": {
                    "Recipients": [{
                        "statusCode": {{statusCode}},
                        "status": "Queued",
                        "messageId": "ATPid_123"
                    }]
                }
            }
            """;

        var result = AfricasTalkingSmsResponseMapper.FromResponse("africas-talking", json);

        result.Success.Should().BeFalse();
        result.Status.Should().Be(expectedStatus);
        result.ErrorCode.Should().Be(statusCode.ToString(System.Globalization.CultureInfo.InvariantCulture));
        result.MayHaveBeenAccepted.Should().BeTrue();
    }

    [Theory]
    [InlineData(403, SmsDeliveryStatus.Undelivered, false)]
    [InlineData(405, SmsDeliveryStatus.Failed, false)]
    [InlineData(500, SmsDeliveryStatus.Failed, true)]
    [InlineData(501, SmsDeliveryStatus.Failed, true)]
    [InlineData(502, SmsDeliveryStatus.Failed, true)]
    public void FromResponse_MapsFailureStatuses(
        int statusCode,
        SmsDeliveryStatus expectedStatus,
        bool expectedTransient)
    {
        var json = $$"""
            {
                "SMSMessageData": {
                    "Recipients": [{
                        "statusCode": {{statusCode}},
                        "status": "ProviderStatus"
                    }]
                }
            }
            """;

        var result = AfricasTalkingSmsResponseMapper.FromResponse("africas-talking", json);

        result.Success.Should().BeFalse();
        result.Status.Should().Be(expectedStatus);
        result.ErrorCode.Should().Be(statusCode.ToString(System.Globalization.CultureInfo.InvariantCulture));
        result.ErrorMessage.Should().Be("ProviderStatus");
        result.IsTransientFailure.Should().Be(expectedTransient);
    }

    [Fact]
    public void FromErrorResponse_ParsesErrorMessage()
    {
        const string json = """
            {
                "code": "InvalidRequest",
                "message": "Invalid API key"
            }
            """;

        var result = AfricasTalkingSmsResponseMapper.FromErrorResponse("africas-talking", 401, json);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("InvalidRequest");
        result.ErrorMessage.Should().Be("Invalid API key");
        result.IsTransientFailure.Should().BeFalse();
    }

    [Fact]
    public void FromErrorResponse_Marks5xxAsAmbiguousTransientFailure()
    {
        var result = AfricasTalkingSmsResponseMapper.FromErrorResponse("africas-talking", 503, "{}");

        result.IsTransientFailure.Should().BeTrue();
        result.MayHaveBeenAccepted.Should().BeTrue();
    }
}
