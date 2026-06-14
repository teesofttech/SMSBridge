using SmsBridge.Abstractions;
using SmsBridge.Webhooks;

namespace SmsBridge.WebhookTests;

public sealed class TwilioWebhookParserTests
{
    private readonly TwilioWebhookParser _parser = new();

    [Fact]
    public void Parse_MapsDeliveredStatus()
    {
        var payload = new Dictionary<string, string>
        {
            ["MessageSid"] = "SM123",
            ["To"] = "+447700900001",
            ["MessageStatus"] = "delivered"
        };

        var evt = _parser.Parse(payload);

        evt.Provider.Should().Be(SmsProviderType.Twilio);
        evt.MessageId.Should().Be("SM123");
        evt.Status.Should().Be(SmsDeliveryStatus.Delivered);
    }

    [Fact]
    public void Parse_MapsFailedStatus()
    {
        var payload = new Dictionary<string, string>
        {
            ["MessageSid"] = "SM456",
            ["MessageStatus"] = "failed"
        };

        var evt = _parser.Parse(payload);

        evt.Status.Should().Be(SmsDeliveryStatus.Failed);
    }

    [Fact]
    public void Parse_MapsUnknownStatusToUnknown()
    {
        var payload = new Dictionary<string, string>
        {
            ["MessageStatus"] = "mystery"
        };

        var evt = _parser.Parse(payload);

        evt.Status.Should().Be(SmsDeliveryStatus.Unknown);
    }
}
