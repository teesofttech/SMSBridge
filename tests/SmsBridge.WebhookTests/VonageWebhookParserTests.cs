using SmsBridge.Abstractions;
using SmsBridge.Webhooks;

namespace SmsBridge.WebhookTests;

public sealed class VonageWebhookParserTests
{
    private readonly VonageWebhookParser _parser = new();

    [Theory]
    [InlineData("2")]
    [InlineData("7")]
    [InlineData("8")]
    public void Parse_MapsTransientDeliveryErrors(string errorCode)
    {
        var payload = new Dictionary<string, string>
        {
            ["messageId"] = "V123",
            ["msisdn"] = "447700900001",
            ["status"] = "failed",
            ["err-code"] = errorCode
        };

        var evt = _parser.Parse(payload);

        evt.Provider.Should().Be(SmsProviderType.Vonage);
        evt.Status.Should().Be(SmsDeliveryStatus.Failed);
        evt.ErrorCode.Should().Be(errorCode);
        evt.IsTransientFailure.Should().BeTrue();
    }

    [Fact]
    public void Parse_MapsAcceptedStatusAsQueued()
    {
        var evt = _parser.Parse(new Dictionary<string, string>
        {
            ["status"] = "accepted",
            ["err-code"] = "0"
        });

        evt.Status.Should().Be(SmsDeliveryStatus.Queued);
        evt.IsTransientFailure.Should().BeFalse();
    }
}
